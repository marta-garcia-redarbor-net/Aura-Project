# Design: Login Popup — OIDC Pipeline Redesign

## Technical Approach

Replace the broken manual OIDC URL construction + MSAL token exchange with the correct
challenge-endpoint pattern. The OIDC middleware owns state/nonce/correlation; the app owns
nothing beyond `/login/challenge` (a one-line `ChallengeAsync` call).

`AuthorizeRouteView` enforces auth at the Blazor layer — removing the HTTP-level `[Authorize]`
so the circuit loads before the user touches login.

## Architecture Decisions

| Decision | Options | Choice | Rationale |
|----------|---------|--------|-----------|
| Auth enforcement layer | HTTP `[Authorize]` vs Blazor `AuthorizeRouteView` | `AuthorizeRouteView` | HTTP-level challenge fires before Blazor circuit — blurred shell is impossible with `[Authorize]` |
| OIDC state/nonce ownership | `OidcUrlBuilder` (manual) vs OIDC middleware via `/login/challenge` | OIDC middleware | Manual construction bypasses correlation cookie; middleware handles CSRF/replay correctly |
| Token storage | `AcquireTokenInteractive` (desktop) vs `SaveTokens=true` in session cookie | Session cookie | `AcquireTokenInteractive` is a desktop primitive — no browser window on the server; `SaveTokens` is the standard server-side approach |
| Callback token exchange | `IConfidentialClientApplication.AcquireTokenByAuthorizationCode` vs middleware-handled | Middleware-handled | OIDC middleware already exchanges the code at `/signin-oidc`; doing it again in the callback is double exchange and breaks auth |
| `OnRedirectToIdentityProvider` suppression | Keep 401 suppression vs remove | Remove | The 401 suppression kills the pipeline for all OIDC routes including `/login/challenge`; it is not needed once `[Authorize]` is off `Index.razor` |

## Data Flow

```
User hits /
    │
    ▼ (no [Authorize] — anonymous load)
Blazor circuit starts → CascadingAuthenticationState → AuthorizeRouteView
    │
    ├── Authenticated ──→ Index.razor (dashboard)
    │
    └── Unauthenticated ──→ RestrictedAccessView (blurred shell + login card)
                                │
                         Click "Sign in with Microsoft"
                                │
                         auth-popup.js: window.open("/login/challenge", ...)
                                │
                         /login/challenge (AllowAnonymous)
                         HttpContext.ChallengeAsync(OpenIdConnect, redirectUri="/authentication/callback")
                                │
                         OIDC middleware: generates state/nonce, sets correlation cookie
                         302 → Entra ID (popup window)
                                │
                         User authenticates in Entra ID
                         302 → /signin-oidc?code=...&state=...
                                │
                         OIDC middleware: validates state, exchanges code, writes auth cookie
                         302 → /authentication/callback
                                │
                         AuthenticationCallback.OnAfterRenderAsync
                         window.opener? → postMessage({type:'auth-success'}) + window.close()
                                │
                         RestrictedAccessView: listenForAuthResult resolves
                         Navigation.Refresh() → AuthorizeRouteView re-evaluates → dashboard
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.UI/Program.cs` | Modify | Remove `OnRedirectToIdentityProvider` suppression; remove `IPublicClientApplication` (Entra branch); remove `IConfidentialClientApplication`; add `/login/challenge` minimal API endpoint before `MapRazorComponents` |
| `src/Aura.UI/Pages/Index.razor` | Modify | Remove `@attribute [Authorize]` and `@using Microsoft.AspNetCore.Authorization` |
| `src/Aura.UI/Components/Auth/RestrictedAccessView.razor.cs` | Modify | `BuildAuthUrl()` returns `"/login/challenge"` constant; remove `IConfiguration`, `OidcUrlBuilder` usage; `HandleFallbackRedirect()` navigates to `"/login/challenge"` |
| `src/Aura.UI/Components/Auth/AuthenticationCallback.razor.cs` | Modify | Remove `IConfiguration`, `IServiceProvider`, `IConfidentialClientApplication`, `ExchangeCodeForTokenAsync`; `OnAfterRenderAsync(firstRender)` → detect `window.opener` → postMessage + close OR redirect to `/` |
| `src/Aura.UI/Services/MsalTokenAcquisitionService.cs` | Modify | Inject `IHttpContextAccessor`; `AcquireTokenAsync` reads `_httpContextAccessor.HttpContext!.GetTokenAsync("access_token")`; remove `IPublicClientApplication` dependency |
| `src/Aura.UI/Services/OidcUrlBuilder.cs` | Delete | Dead — middleware owns URL construction |
| `src/Aura.UI/Components/RedirectToLogin.razor` | Delete | Dead — `RestrictedAccessView` handles all unauthenticated cases |

## Interfaces / Contracts

```csharp
// /login/challenge — added in Aura.UI Program.cs BEFORE app.MapRazorComponents
app.MapGet("/login/challenge", async (HttpContext ctx) =>
{
    var props = new AuthenticationProperties
    {
        RedirectUri = "/authentication/callback"
    };
    await ctx.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, props);
}).AllowAnonymous();

// MsalTokenAcquisitionService — simplified ctor and AcquireTokenAsync
public MsalTokenAcquisitionService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
{
    _httpContextAccessor = httpContextAccessor;
    var clientId = configuration["AzureAd:ClientId"] ?? throw new InvalidOperationException("...");
    _meetingAlertsScope = [$"api://{clientId}/MeetingAlerts"];
}

public async Task<string> AcquireTokenAsync(CancellationToken cancellationToken = default)
    => await _httpContextAccessor.HttpContext!.GetTokenAsync("access_token")
       ?? throw new InvalidOperationException("No access_token in session. Ensure SaveTokens=true.");

// RestrictedAccessView.BuildAuthUrl — simplified
private string BuildAuthUrl() => "/login/challenge";

// AuthenticationCallback.OnAfterRenderAsync — simplified
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;
    var isPopup = await JSRuntime.InvokeAsync<bool>("eval", "window.opener !== null && !window.opener.closed");
    if (isPopup)
    {
        await JSRuntime.InvokeVoidAsync("eval", "window.opener.postMessage({type:'auth-success'},'*'); window.close();");
    }
    else
    {
        Navigation.NavigateTo("/", forceLoad: true);
    }
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `RestrictedAccessView` renders blurred shell without triggering auth | bUnit: render unauthenticated; assert login card present, no redirect |
| Unit | Button click calls `OpenMicrosoftLoginPopupAsync` with `"/login/challenge"` | bUnit: mock `IAuthPopupService`; click button; verify call argument |
| Unit | `AuthenticationCallback` with `window.opener` → calls postMessage JS | bUnit: mock `IJSRuntime`; assert `eval` called with `postMessage` + `window.close()` |
| Unit | `MsalTokenAcquisitionService` reads from `HttpContext.GetTokenAsync` | Unit: mock `IHttpContextAccessor`; seed `access_token`; assert returned |
| Integration | `GET /login/challenge` returns 302 with `Location` pointing to Entra and sets correlation cookie | `WebApplicationFactory<UiMarker>` + `HttpClient` (no redirect follow); assert 302 + `Set-Cookie: .AspNetCore.Correlation.*` |

## Migration / Rollout

No data migration. Config changes required before deploy:

1. Verify Entra ID App Registration has `/signin-oidc` as a valid redirect URI (already registered per proposal).
2. Verify `AzureAd:ClientSecret` is present in environment secrets — required by OIDC middleware for code exchange (was required before; no change).
3. Remove `AzureAd:RedirectBase` from config if only used by `OidcUrlBuilder` — confirm no other consumers first.

Rollback: revert `Program.cs` (restore `OnRedirectToIdentityProvider`, remove `/login/challenge`), restore `[Authorize]` on `Index.razor`, restore `MsalTokenAcquisitionService`, restore `OidcUrlBuilder`. No DB changes.

## Open Questions

- [ ] `AzureAd:RedirectBase` config key: only consumed by `OidcUrlBuilder` and `RestrictedAccessView.BuildAuthUrl`. After this change both are removed/simplified — confirm no other consumer before deleting the key from `appsettings`.
- [ ] Cookie size: `SaveTokens=true` is already set in `Program.cs` today. Confirm token size fits within 4 KB before deploying to production.
