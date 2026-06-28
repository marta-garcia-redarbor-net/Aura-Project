# Tasks: Login Popup — OIDC Pipeline Redesign

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~350–500 |
| 600-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | single-pr |
| Chain strategy | size-exception |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: size-exception
400-line budget risk: Low

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Full OIDC pipeline redesign — all phases | PR 1 | base: main; all tests + deletions + wiring included |

---

## Phase 1: Foundation — Auth Pipeline (Program.cs)

- [x] 1.1 **RED** — `tests/Aura.IntegrationTests/Auth/LoginChallengeEndpointTests.cs`: `GET /login/challenge` anonymous → 302 with `Location` pointing to Entra + `Set-Cookie: .AspNetCore.Correlation.*`. Size: M
- [x] 1.2 **GREEN** — `src/Aura.UI/Program.cs`: Replace `AddMicrosoftIdentityWebApp` with `AddAuthentication(CookieDefaults).AddCookie(...).AddOpenIdConnect(...)` with `SaveTokens=true`; remove `OnRedirectToIdentityProvider` suppression; remove `IPublicClientApplication` + `IConfidentialClientApplication` DI registrations. Size: M
- [x] 1.3 **GREEN** — `src/Aura.UI/Program.cs`: Add `app.MapGet("/login/challenge", ...)` with `AllowAnonymous()` calling `ChallengeAsync(OpenIdConnect, redirectUri="/authentication/callback")` — placed **before** `MapRazorComponents`. Size: S
- [x] 1.4 Run `dotnet test Aura.sln` — integration test 1.1 passes (GREEN gate). Size: S

## Phase 2: Core — Remove [Authorize] + Simplify RestrictedAccessView

- [x] 2.1 **RED** — `tests/Aura.UnitTests/UI/RestrictedAccessViewTests.cs`: bUnit render unauthenticated → assert `data-testid="restricted-access-view"` present, no redirect, URL stays `/`. Size: S
- [x] 2.2 **RED** — bUnit test: button click calls `IAuthPopupService.OpenMicrosoftLoginPopupAsync` with argument `"/login/challenge"`. Size: S
- [x] 2.3 **GREEN** — `src/Aura.UI/Pages/Index.razor`: Remove `@attribute [Authorize]` and `@using Microsoft.AspNetCore.Authorization`. Size: S
- [x] 2.4 **GREEN** — `src/Aura.UI/Components/Auth/RestrictedAccessView.razor.cs`: `BuildAuthUrl()` returns `"/login/challenge"` constant; remove `IConfiguration` and `OidcUrlBuilder` injection; `HandleFallbackRedirect()` navigates to `"/login/challenge"`. Size: S
- [x] 2.5 Run `dotnet test Aura.sln` — unit tests 2.1 and 2.2 pass (GREEN gate). Size: S

## Phase 3: Core — AuthenticationCallback + MsalTokenAcquisitionService

- [x] 3.1 **RED** — `tests/Aura.UnitTests/UI/AuthenticationCallbackTests.cs`: bUnit mock `IJSRuntime`; `window.opener` is set → assert `eval` called with string containing `postMessage({type:'auth-success'}` AND `window.close()`. Size: S
- [x] 3.2 **RED** — bUnit test: `window.opener` is null → assert `Navigation.NavigateTo("/", forceLoad: true)` called; no `postMessage`. Size: S
- [x] 3.3 **GREEN** — `src/Aura.UI/Components/Auth/AuthenticationCallback.razor.cs`: Remove `IConfiguration`, `IServiceProvider`, `IConfidentialClientApplication`, `ExchangeCodeForTokenAsync`; implement `OnAfterRenderAsync(firstRender)` with popup-detection + `postMessage`+`close` or fallback `NavigateTo`. Size: M
- [x] 3.4 **RED** — `tests/Aura.UnitTests/Infrastructure/MsalTokenAcquisitionServiceTests.cs`: mock `IHttpContextAccessor` seeded with `access_token` → `AcquireTokenAsync` returns the seeded token without calling `AcquireTokenInteractive`. Size: S
- [x] 3.5 **RED** — unit test: `AzureAd:ClientId = "test-id"` → `MeetingAlerts` scope equals `"api://test-id/MeetingAlerts"` (no hardcoded GUID). Size: S
- [x] 3.6 **GREEN** — `src/Aura.UI/Services/MsalTokenAcquisitionService.cs`: Inject `IHttpContextAccessor`; `AcquireTokenAsync` reads `HttpContext.GetTokenAsync("access_token")`; scope uses `configuration["AzureAd:ClientId"]`; remove `IPublicClientApplication` dependency. Size: M
- [x] 3.7 Run `dotnet test Aura.sln` — all RED tests from phases 3.1–3.6 are GREEN. Size: S

## Phase 4: Cleanup — Delete Dead Code

- [x] 4.1 **Delete** `src/Aura.UI/Services/OidcUrlBuilder.cs` — dead, replaced by `/login/challenge`. Size: S
- [x] 4.2 **Delete** `src/Aura.UI/Components/RedirectToLogin.razor` — dead, `RestrictedAccessView` covers all unauthenticated cases. Size: S
- [x] 4.3 Search codebase for `OidcUrlBuilder` and `RedirectToLogin` usages — confirm zero remaining references before deleting. Size: S
- [x] 4.4 Confirm `AzureAd:RedirectBase` config key has no consumers other than deleted files; remove from `appsettings` if confirmed. Size: S
- [x] 4.5 Run `dotnet test Aura.sln` — full suite green; no compilation errors from deleted types. Size: S

## Phase 5: Final Verification

- [x] 5.1 Run `dotnet test Aura.sln` — all unit + integration tests pass. Size: S
- [ ] 5.2 Manual E2E: unauthenticated user loads `/` → blurred shell renders immediately (no redirect, no blank page). Size: S
- [ ] 5.3 Manual E2E: click "Sign in with Microsoft" → popup opens `/login/challenge` → Entra auth completes → callback posts `auth-success` → opener refreshes → dashboard renders. Size: S
- [ ] 5.4 Verify no 401 is returned to the browser before user clicks login. Size: S
