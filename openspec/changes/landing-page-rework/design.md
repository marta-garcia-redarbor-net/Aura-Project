# Design: Landing Page Rework

## Technical Approach

Transform the current authenticated dashboard at `/` into a public landing page that converts first-time visitors, while moving the dashboard to `/dashboard`. The landing page uses the existing Stitch design (dark theme, Tailwind classes matching `stitch-dashboard.css` tokens) and provides two CTAs: Microsoft login (popup flow) and Demo Mode (fake auth with demo claims).

**Key architectural pattern**: Use Blazor's `[AllowAnonymous]` attribute on the landing page component to bypass `AuthorizeRouteView`, combined with client-side auth state check for auto-redirect of authenticated users.

## Architecture Decisions

### Decision: Anonymous Route Strategy

**Choice**: Apply `[AllowAnonymous]` attribute to `LandingPage.razor` component, keeping `AuthorizeRouteView` in `Routes.razor` unchanged.

**Alternatives considered**:
- Separate `Router` for anonymous routes → adds complexity, duplicates routing logic
- Modify `Routes.razor` to conditionally use `RouteView` vs `AuthorizeRouteView` → breaks existing pattern, harder to maintain
- Middleware-based anonymous route detection → overkill for single page, mixes concerns

**Rationale**: Blazor's `[AllowAnonymous]` is the idiomatic way to mark components as public. It works with `AuthorizeRouteView` without modification, keeps routing logic centralized, and follows ASP.NET Core conventions. The landing page uses `AnonymousLayout` (already used for 404/error pages) which renders just `@Body` without sidebar or header chrome — no `AuthorizeView` wrapper needed since the page is entirely public. This approach supersedes the initial plan to modify `Routes.razor`; no changes to `Routes.razor` are required.

### Decision: Demo Mode Claim Architecture

**Choice**: `/login/demo` endpoint sets `aura_demo_mode=true` claim on auth cookie. UI components check `context.User.HasClaim("aura_demo_mode", "true")` via `AuthorizeView` or `AuthenticationState` cascading parameter.

**Alternatives considered**:
- Keep config-driven `UseEntraId` check → couples UI to deployment config, doesn't work for per-user demo mode
- Session-based demo flag → requires session state, breaks Blazor Server circuit model
- Query parameter `?demo=true` → not persistent across navigation, security risk

**Rationale**: Claims-based approach decouples demo mode from config, allows per-user demo sessions, works with Blazor Server's stateless circuit model, and integrates naturally with existing `AuthorizeView` pattern. The claim is set once during fake login and persists for the cookie lifetime.

### Decision: LoginButton Component Reusability

**Choice**: Create `LoginButton.razor` as a reusable CTA component that accepts `ButtonText` and `CssClass` parameters, internally using `IAuthPopupService` for popup flow.

**Alternatives considered**:
- Inline login logic in LandingPage → duplicates `RestrictedAccessView` logic, harder to test
- Single monolithic landing page with all logic → violates component reusability, harder to maintain

**Rationale**: Extracting `LoginButton` follows atomic design principles, enables reuse across landing page sections (header, hero, bottom CTA), and isolates popup auth logic for easier testing. The component wraps `IAuthPopupService` (already registered in DI) and handles the popup → callback → redirect flow.

## Data Flow

### Authenticated User Auto-Redirect

```
User (authenticated) → GET /
    ↓
LandingPage.OnInitializedAsync()
    ↓
Check AuthenticationState (cascading parameter)
    ↓
If authenticated → Navigation.NavigateTo("/dashboard", forceLoad: true)
    ↓
Dashboard loads at /dashboard
```

### Popup Auth Flow (Unchanged)

```
LandingPage → LoginButton click
    ↓
AuthPopupService.OpenMicrosoftLoginPopupAsync("/login/challenge?popup=true")
    ↓
Popup opens → OIDC challenge → /signin-oidc → /authentication/callback?popup=true
    ↓
AuthenticationCallback posts auth-success to opener → closes popup
    ↓
LoginButton receives success → Navigation.NavigateTo("/dashboard", forceLoad: true)
```

### Demo Auth Flow (New)

```
LandingPage → "Explore Demo Mode" button click
    ↓
Navigation.NavigateTo("/login/demo", forceLoad: true)
    ↓
/login/demo endpoint (Minimal API):
    - Creates ClaimsPrincipal with demo claims (name, email, role, aura_demo_mode=true)
    - HttpContext.SignInAsync("Cookies", principal)
    - Redirect to /dashboard
    ↓
Dashboard loads with demo claims → UI shows demo controls
```

## Route Design

| Route | Before | After | Auth Required | Component |
|-------|--------|-------|---------------|-----------|
| `/` | PriorityDashboard | LandingPage | No (AllowAnonymous) | `LandingPage.razor` |
| `/dashboard` | N/A | PriorityDashboard | Yes | `PriorityDashboard.razor` |
| `/test-dashboard` | Index.razor | Index.razor | Yes | `Pages/Index.razor` (unchanged) |
| `/login/demo` | N/A | N/A | No | Minimal API endpoint |
| `/login/challenge` | N/A | N/A | No | Minimal API endpoint (unchanged) |
| `/login/dev` | N/A | N/A | No | Minimal API endpoint (unchanged) |
| `/logout` | N/A | N/A | No | Minimal API endpoint (redirect target changes) |
| `/authentication/callback` | N/A | N/A | No | `AuthenticationCallback.razor` (redirect target changes) |

## Component Design

### LandingPage.razor (New)

**Location**: `src/Aura.UI/Components/Pages/LandingPage.razor`

**Structure**:
```
@page "/"
@attribute [AllowAnonymous]
@layout AnonymousLayout
@inject NavigationManager Navigation
@inject IAuthPopupService AuthPopupService
@inject IJSRuntime JSRuntime

<PageTitle>Aura | AI-Powered Engineering Dashboard</PageTitle>

<!-- Fixed Header -->
<header class="landing-header">
    <div class="landing-header__brand">Aura</div>
    <LoginButton ButtonText="Login / Access Aura" CssClass="landing-header__cta" />
</header>

<!-- Hero Section -->
<section class="landing-hero">
    <h1 class="landing-hero__title">...</h1>
    <p class="landing-hero__subtitle">...</p>
    <div class="landing-hero__ctas">
        <LoginButton ButtonText="Login / Access Aura" CssClass="btn-primary" />
        <button class="btn-secondary" @onclick="HandleDemoLogin">Explore Demo Mode</button>
    </div>
</section>

<!-- Problem/Solution Grid -->
<section class="landing-problems">...</section>

<!-- Features Bento Grid -->
<section class="landing-features">...</section>

<!-- Bottom CTA -->
<section class="landing-cta">
    <LoginButton ButtonText="Get Started" CssClass="btn-primary" />
</section>

<!-- Footer -->
<footer class="landing-footer">...</footer>

@code {
    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Auto-redirect authenticated users to dashboard
        if (AuthenticationState is not null)
        {
            var authState = await AuthenticationState;
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                Navigation.NavigateTo("/dashboard", forceLoad: true);
            }
        }
    }

    private void HandleDemoLogin()
    {
        Navigation.NavigateTo("/login/demo", forceLoad: true);
    }
}
```

**Key points**:
- Uses `AnonymousLayout` (no sidebar/header chrome)
- `[AllowAnonymous]` bypasses `AuthorizeRouteView`
- Checks `AuthenticationState` on init to auto-redirect authenticated users
- `LoginButton` component handles popup auth flow
- Demo button navigates to `/login/demo` endpoint

### LoginButton.razor (New)

**Location**: `src/Aura.UI/Components/Auth/LoginButton.razor`

**Structure**:
```
@inject IAuthPopupService AuthPopupService
@inject IJSRuntime JSRuntime
@inject NavigationManager Navigation

<button class="@CssClass" @onclick="HandleClick" disabled="@_loading">
    @if (_loading)
    {
        <span>Signing in...</span>
    }
    else
    {
        @ButtonText
    }
</button>

@if (_popupBlocked)
{
    <div class="login-button__blocked">
        Pop-ups blocked. <a href="/login/challenge">Click here to sign in directly</a>.
    </div>
}

@code {
    [Parameter] public string ButtonText { get; set; } = "Login";
    [Parameter] public string CssClass { get; set; } = "btn-primary";

    private bool _loading;
    private bool _popupBlocked;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await AuthPopupService.InitializeAsync(JSRuntime);
        }
    }

    private async Task HandleClick()
    {
        try
        {
            _loading = true;
            _popupBlocked = false;
            StateHasChanged();

            await AuthPopupService.OpenMicrosoftLoginPopupAsync("/login/challenge?popup=true");
            var result = await AuthPopupService.WaitForPopupResultAsync(CancellationToken.None);

            if (result?.Success == true)
            {
                Navigation.NavigateTo("/dashboard", forceLoad: true);
            }
        }
        catch (InvalidOperationException)
        {
            _popupBlocked = true;
        }
        catch (JSException)
        {
            _popupBlocked = true;
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }
}
```

**Key points**:
- Reusable across landing page sections
- Wraps `IAuthPopupService` (same service used by `RestrictedAccessView`)
- Handles popup blocked scenario with fallback link
- Redirects to `/dashboard` (not `/`) after successful auth

**Note on multiple LoginButton instances**: The landing page renders three `LoginButton` components (header, hero, bottom CTA). Each calls `AuthPopupService.InitializeAsync()` in `OnAfterRenderAsync(firstRender: true)`. The implementation of `InitializeAsync` in `AuthPopupService` is idempotent — it only sets up the JS module reference and the `postMessage` listener once. The first call initializes; subsequent calls are no-ops. No additional guard logic is required.

### PriorityDashboard.razor (Modified)

**Change**: `@page "/"` → `@page "/dashboard"`

**Impact**: All navigation references to `/` must update to `/dashboard`:
- `AuthenticationCallback.razor.cs` line 62: `Navigation.NavigateTo("/")` → `Navigation.NavigateTo("/dashboard")`
- `RestrictedAccessView.razor.cs` line 62: `Navigation.NavigateTo("/")` → `Navigation.NavigateTo("/dashboard")`
- `Program.cs` line 309 (logout redirect): `ctx.Response.Redirect("/")` → `ctx.Response.Redirect("/")` (unchanged — logout should go to landing page)
- `Program.cs` line 341 (dev login redirect): `ctx.Response.Redirect("/")` → `ctx.Response.Redirect("/dashboard")`

### Demo Mode Implementation

**New endpoint** in `Program.cs`:

```csharp
app.MapGet("/login/demo", async (HttpContext ctx) =>
{
    var claims = new[]
    {
        new Claim(ClaimTypes.Name, "Demo User"),
        new Claim(ClaimTypes.Email, "demo@aura.local"),
        new Claim(ClaimTypes.Role, "Demo"),
        new Claim("aura_demo_mode", "true"),
        new Claim("oid", "demo-user-001")
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    ctx.Response.Redirect("/dashboard");
}).AllowAnonymous();
```

### Demo-to-API Authentication

The demo user's Blazor Server circuit authenticates outgoing API calls via the existing `DevAccessTokenHandler` (registered in `ForwardedAccessTokenHandler.cs`). When the Blazor Server circuit creates an `HttpClient` through the `AuraApi` named client, the handler intercepts requests and attaches a mock JWT obtained from `POST /api/auth/mock-login`. This mechanism works identically for both dev-mode cookies and demo-mode cookies — no new API auth path is needed.

The `aura_demo_mode` claim is a UI-only signal; API authorization continues using the demo user's role claim (`ClaimTypes.Role = "Demo"`) for endpoint-level decisions.

**UI changes** in `PriorityDashboard.razor`:

Replace config-driven demo check:
```csharp
// BEFORE
private async Task CheckDemoStatusAsync()
{
    var client = HttpClientFactory.CreateClient("AuraApi");
    var status = await client.GetFromJsonAsync<DemoStatus>("api/demo/status");
    _demoAvailable = status?.Enabled == true;
}
```

With claim-based check:
```csharp
// AFTER
[CascadingParameter]
private Task<AuthenticationState>? AuthenticationState { get; set; }

private async Task CheckDemoStatusAsync()
{
    if (AuthenticationState is not null)
    {
        var authState = await AuthenticationState;
        _demoAvailable = authState.User.HasClaim("aura_demo_mode", "true");
    }
}
```

**RestrictedAccessView.razor.cs** changes:
- Remove `_useEntraId` field and config injection
- Remove conditional dev login button (now handled by landing page)
- Keep Microsoft login button (popup flow)

## Interfaces / Contracts

### New Components

**LandingPage.razor**
- Route: `@page "/"`
- Attribute: `[AllowAnonymous]`
- Layout: `AnonymousLayout`
- Injects: `NavigationManager`, `IAuthPopupService`, `IJSRuntime`
- Cascading parameter: `Task<AuthenticationState>`

**LoginButton.razor**
- Parameters: `ButtonText` (string), `CssClass` (string)
- Injects: `IAuthPopupService`, `IJSRuntime`, `NavigationManager`
- Public method: None (self-contained click handler)

### New Endpoint

**GET /login/demo**
- Auth: Anonymous
- Behavior: Creates demo claims cookie, redirects to `/dashboard`
- Claims set: `name`, `email`, `role=Demo`, `aura_demo_mode=true`, `oid=demo-user-001`

### Modified Components

**PriorityDashboard.razor**
- Route: `@page "/dashboard"` (was `@page "/"`)
- Demo check: `AuthenticationState.User.HasClaim("aura_demo_mode", "true")` (was `api/demo/status` call)

**AuthenticationCallback.razor.cs**
- Redirect target: `/dashboard` (was `/`)

**RestrictedAccessView.razor.cs**
- Redirect target after auth: `/dashboard` (was `/`)
- Removed: `_useEntraId` config check, dev login button

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| **Unit** | LandingPage auto-redirect for authenticated users | bUnit: render with mocked `AuthenticationState` (authenticated), assert `NavigationManager.Uri` is `/dashboard` |
| **Unit** | LandingPage renders for anonymous users | bUnit: render with anonymous `AuthenticationState`, assert landing page sections visible |
| **Unit** | LoginButton popup flow | bUnit: mock `IAuthPopupService`, simulate popup success, assert navigation to `/dashboard` |
| **Unit** | LoginButton popup blocked fallback | bUnit: mock `IAuthPopupService` to throw `InvalidOperationException`, assert fallback link rendered |
| **Unit** | PriorityDashboard demo claim check | bUnit: render with `AuthenticationState` containing `aura_demo_mode=true` claim, assert demo buttons visible |
| **Unit** | PriorityDashboard non-demo user | bUnit: render with `AuthenticationState` without demo claim, assert demo buttons hidden |
| **Integration** | `/login/demo` endpoint creates cookie | `WebApplicationFactory`: GET `/login/demo`, assert response is redirect to `/dashboard`, assert cookie contains `aura_demo_mode` claim |
| **Integration** | `/logout` redirects to landing page | `WebApplicationFactory`: GET `/logout`, assert redirect to `/` |
| **Integration** | `/login/dev` redirects to `/dashboard` | `WebApplicationFactory`: GET `/login/dev`, assert redirect to `/dashboard` |
| **E2E** | Landing page loads for anonymous user | Playwright: navigate to `/`, assert hero section visible, assert login button present |
| **E2E** | Authenticated user auto-redirects | Playwright: login via `/login/dev`, navigate to `/`, assert redirected to `/dashboard` |
| **E2E** | Demo mode login flow | Playwright: click "Explore Demo Mode", assert redirected to `/dashboard`, assert demo controls visible |
| **Architecture** | LandingPage has `[AllowAnonymous]` | NetArchTest: assert `LandingPage` type has `AllowAnonymousAttribute` |
| **Architecture** | PriorityDashboard route is `/dashboard` | Reflection: assert `PriorityDashboard` has `RouteAttribute` with template `/dashboard` |

**Test command**: `dotnet test Aura.sln --collect:"XPlat Code Coverage"`

**Coverage target**: 80% (per `openspec/config.yaml`)

## Migration / Rollout

### Data Migration
No data migration required. This is a UI/routing change only.

### Session Concerns
- Existing user sessions with auth cookies remain valid
- Cookie structure unchanged (same claims, same scheme)
- Demo users created via `/login/demo` get new `aura_demo_mode` claim — no impact on existing users

### Rollout Strategy
1. Deploy changes (landing page, dashboard route, demo endpoint)
2. Existing bookmarks to `/` will show landing page (expected behavior)
3. Authenticated users at `/` auto-redirect to `/dashboard` (seamless)
4. No feature flags needed — change is atomic and reversible

### Rollback Plan
Per proposal: revert `PriorityDashboard` route to `@page "/"`, remove `LandingPage.razor` and `LoginButton.razor`, restore all redirects from `/dashboard` to `/`, remove `/login/demo` endpoint, restore config-driven demo mode checks.

## Open Questions

- [ ] **None** — all technical decisions resolved based on codebase analysis

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.UI/Components/Pages/LandingPage.razor` | Create | Public landing page with Stitch design, `[AllowAnonymous]`, auto-redirect for authenticated users |
| `src/Aura.UI/Components/Auth/LoginButton.razor` | Create | Reusable login CTA component wrapping `IAuthPopupService` |
| `src/Aura.UI/Components/Dashboard/PriorityDashboard.razor` | Modify | Route `@page "/"` → `@page "/dashboard"`, demo check via claim instead of API call |
| `src/Aura.UI/Program.cs` | Modify | Add `/login/demo` endpoint, update `/login/dev` redirect to `/dashboard` |
| `src/Aura.UI/Components/Auth/AuthenticationCallback.razor.cs` | Modify | Redirect target `/` → `/dashboard` (line 62) |
| `src/Aura.UI/Components/Auth/RestrictedAccessView.razor.cs` | Modify | Redirect target `/` → `/dashboard` (line 62), remove `_useEntraId` config check and dev login button |
| `tests/Aura.UnitTests/Landing/LandingPageTests.cs` | Create | bUnit tests for landing page auto-redirect and rendering |
| `tests/Aura.UnitTests/Auth/LoginButtonTests.cs` | Create | bUnit tests for LoginButton popup flow and fallback |
| `tests/Aura.UnitTests/Dashboard/PriorityDashboardDemoClaimTests.cs` | Create | bUnit tests for demo claim-based visibility |
| `tests/Aura.IntegrationTests/Auth/DemoLoginEndpointTests.cs` | Create | Integration tests for `/login/demo` endpoint |
| `tests/Aura.IntegrationTests/Auth/LogoutRedirectTests.cs` | Create | Integration tests for logout redirect to `/` |
| `tests/Aura.E2E/Landing/LandingPageE2ETests.cs` | Create | Playwright E2E tests for landing page flows |
