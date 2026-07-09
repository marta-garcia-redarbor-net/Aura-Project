# Tasks: Landing Page Rework

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~650-700 (8 new files ~580 lines + 4 modified files ~62 lines + existing test updates ~30 lines) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (Foundation: route + redirects) → PR 2 (Landing page + demo auth) → PR 3 (Tests + cleanup) |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Route migration + redirect wiring | PR 1 | Base: main. Dashboard → `/dashboard`, all redirects updated, existing tests fixed. Standalone mergeable. |
| 2 | Landing page + LoginButton + `/login/demo` endpoint | PR 2 | Base: PR 1 branch or main. New components + demo endpoint + unit/integration tests. |
| 3 | Demo claim migration + E2E + architecture tests | PR 3 | Base: PR 2 branch or main. Replace config-driven demo checks, E2E tests, architecture guards. |

## Phase 1: Foundation — Route Migration & Redirect Wiring

- [x] 1.1 Change `src/Aura.UI/Components/Dashboard/PriorityDashboard.razor` route from `@page "/"` to `@page "/dashboard"`
- [x] 1.2 Update `src/Aura.UI/Components/Auth/AuthenticationCallback.razor.cs` line 62: `Navigation.NavigateTo("/")` → `Navigation.NavigateTo("/dashboard")`
- [x] 1.3 Update `src/Aura.UI/Components/Auth/RestrictedAccessView.razor.cs` line 62: `Navigation.NavigateTo("/")` → `Navigation.NavigateTo("/dashboard")`
- [x] 1.4 Update `src/Aura.UI/Program.cs` line 341 (`/login/dev` redirect): `ctx.Response.Redirect("/")` → `ctx.Response.Redirect("/dashboard")`
- [x] 1.5 Update existing `tests/Aura.UnitTests/UI/AuthenticationCallbackTests.cs` — assert redirect to `/dashboard` instead of `/`
- [x] 1.6 Update existing `tests/Aura.UnitTests/UI/RestrictedAccessViewTests.cs` — assert redirect to `/dashboard` instead of `/`
- [x] 1.7 Update `tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs` — navigate to `/dashboard` instead of `/` for dashboard tests

## Phase 2: Core Implementation — Landing Page, LoginButton & Demo Endpoint

- [x] 2.1 Create `src/Aura.UI/Components/Pages/LandingPage.razor` — `@page "/"`, `[AllowAnonymous]`, `@layout AnonymousLayout`, auto-redirect for authenticated users via `AuthenticationState` cascading parameter, Stitch dark-theme sections (header, hero with dual CTAs, problem/solution grid, features bento grid, bottom CTA, footer), `data-testid` attributes per spec
- [x] 2.2 Create `src/Aura.UI/Components/Auth/LoginButton.razor` — reusable CTA with `ButtonText`/`CssClass` parameters, wraps `IAuthPopupService` popup flow, handles popup-blocked fallback, redirects to `/dashboard` on success
- [x] 2.3 Add `/login/demo` endpoint in `src/Aura.UI/Program.cs` — `MapGet("/login/demo")`, `[AllowAnonymous]`, creates `ClaimsPrincipal` with demo claims (name, email, role=Demo, `aura_demo_mode=true`, oid), `SignInAsync` with cookie scheme, redirect to `/dashboard`
- [x] 2.4 Verify `/logout` endpoint in `Program.cs` still redirects to `/` (landing page) — no change needed, confirm existing behavior

## Phase 3: Demo Mode Claim Migration

- [x] 3.1 Replace config-driven demo check in `PriorityDashboard.razor` — remove `api/demo/status` HTTP call, add `[CascadingParameter] Task<AuthenticationState>`, check `authState.User.HasClaim("aura_demo_mode", "true")` to set `_demoAvailable`
- [x] 3.2 Remove `_useEntraId` field and `Configuration.GetValue<bool>("UseEntraId")` from `RestrictedAccessView.razor.cs` — remove `HandleDevLogin()` method and dev login button from markup
- [x] 3.3 Update `RestrictedAccessView.razor` markup — replace inline login card with link/button to `/` (landing page) with `data-testid="restricted-go-login-btn"`, remove Microsoft login popup flow (now on landing page)

## Phase 4: Testing

- [x] 4.1 Create `tests/Aura.UnitTests/Landing/LandingPageTests.cs` — bUnit: (a) anonymous user sees all landing sections, (b) authenticated user auto-redirects to `/dashboard`, (c) demo button navigates to `/login/demo`
- [x] 4.2 Create `tests/Aura.UnitTests/Auth/LoginButtonTests.cs` — bUnit: (a) popup success navigates to `/dashboard`, (b) `InvalidOperationException` shows fallback link, (c) `JSException` shows fallback link, (d) loading state disables button
- [x] 4.3 Create `tests/Aura.UnitTests/Dashboard/PriorityDashboardDemoClaimTests.cs` — bUnit: (a) `aura_demo_mode=true` claim shows demo controls, (b) absent claim hides demo controls, (c) config `DemoMode__Enabled=true` without claim does NOT show controls
- [x] 4.4 Create `tests/Aura.IntegrationTests/Auth/DemoLoginEndpointTests.cs` — WebApplicationFactory: (a) GET `/login/demo` returns 302 to `/dashboard`, (b) response sets cookie with `aura_demo_mode=true` claim, (c) endpoint accessible without auth
- [x] 4.5 Create `tests/Aura.IntegrationTests/Auth/LogoutRedirectTests.cs` — WebApplicationFactory: GET `/logout` redirects to `/`
- [x] 4.6 Create `tests/Aura.E2E/Landing/LandingPageE2ETests.cs` — Playwright: (a) anonymous at `/` sees hero + login button, (b) authenticated at `/` redirects to `/dashboard`, (c) "Explore Demo Mode" navigates to `/login/demo` then `/dashboard` with demo controls visible
- [x] 4.7 Add architecture tests — assert `LandingPage` has `[AllowAnonymous]` attribute, assert `PriorityDashboard` has `[Route("/dashboard")]`

## Phase 5: Cleanup & Verification

- [x] 5.1 Remove unused `IConfiguration` injection from `RestrictedAccessView.razor.cs` if no longer needed after config check removal
- [x] 5.2 Run `dotnet test Aura.sln` — all existing + new tests pass
- [x] 5.3 Run `dotnet build` — no warnings related to changed files
- [x] 5.4 Verify no remaining references to old `/` route for dashboard navigation (grep for `NavigateTo("/")` in UI project)
