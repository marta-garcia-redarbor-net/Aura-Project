# Design: Graph UI Polish

## Technical Approach

Three pure-UI changes in Blazor Server (.NET 9) — no backend, no new DI, no new API endpoints. All changes operate within the existing `Aura.UI` component tree and follow established patterns: `AuthorizeView` for auth, `@inject` for DI, config-driven auth mode detection.

## Architecture Decisions

### Decision: Health page follows Index.razor pattern

**Choice**: Mirror `Pages/Index.razor` — `@page` directive + `AuthorizeView` wrapper + `NotAuthorized` fallback to `RestrictedAccessView`.
**Alternatives considered**: Create a shared layout component; use `CascadingAuthenticationState` without explicit `AuthorizeView`.
**Rationale**: The proposal explicitly mandates following the existing `Index.razor` pattern. Consistency with the existing `/test-dashboard` page.

### Decision: Auth mode detection via IConfiguration

**Choice**: Inject `IConfiguration` and read `UseEntraId` boolean — same as `RestrictedAccessView.razor.cs` line 42.
**Alternatives considered**: Use `AuthenticationStateProvider` to infer scheme from claims; hardcode scheme.
**Rationale**: The project already uses `Configuration.GetValue<bool>("UseEntraId")` in `RestrictedAccessView`. This is the established pattern. Config value is reliable and avoids async auth state resolution in a button handler.

### Decision: SignOutAsync with scheme branching

**Choice**: Branch on `_useEntraId` — if true, call `SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme)`; if false, call `SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)`. Redirect to `/`.
**Alternatives considered**: Use `HttpContext.SignOutAsync()` with a single scheme; challenge-based signout.
**Rationale**: OIDC signout requires clearing the OIDC session state, not just the cookie. Dev mode only has cookie auth. Both schemes are registered in `Program.cs` (lines 45, 156).

## Data Flow

### Change 1 — Dashboard Reorder

```
PriorityDashboard.razor (current):
  [header] → [loading/error/else] → [grid + SyncButton + RankedSummaryList] → [panels...]

PriorityDashboard.razor (after):
  [header] → [loading/error/else] → [RankedSummaryList + grid + SyncButton] → [panels...]
```

RankedSummaryList moves before the connector-cards-grid div. SyncButton stays adjacent to the grid. No data flow change — same `_summaryItems` and `_connectors` state.

### Change 2 — Health Page

```
Sidebar.razor                Pages/Health.razor
┌─────────┐                 ┌──────────────────────────┐
│ Health   │──href="/health"→│ AuthorizeView             │
│ (a tag)  │                 │   ├ GraphConnectorStatus  │
└─────────┘                 │   ├ SystemStatusPanel     │
                            │   └ ModuleProgressPanel   │
                            └──────────────────────────┘
```

Three panels self-inject their API clients (`IGraphConnectorApiClient`, `ISystemStatusApiClient`, `IModuleProgressApiClient`) — all already registered in `Program.cs` (lines 186-189). No new DI needed.

### Change 3 — Logout Button

```
Header.razor
┌──────────────────────────────────────────────┐
│ [brand] [nav]          [icons] [user] [LOGOUT]│
└──────────────────────────────────────────────┘
         │
         ▼ HandleSignOut()
    IConfiguration → UseEntraId?
         ├─ true  → SignOutAsync(OIDC) → NavigateTo("/")
         └─ false → SignOutAsync(Cookie) → NavigateTo("/")
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.UI/Components/Dashboard/PriorityDashboard.razor` | Modify | Move `<RankedSummaryList>` before `connector-cards-grid` div (line 50 → before line 38) |
| `src/Aura.UI/Pages/Health.razor` | Create | New page at `/health` with AuthorizeView, containing GraphConnectorStatusPanel, SystemStatusPanel, ModuleProgressPanel |
| `src/Aura.UI/Components/Layout/Sidebar.razor` | Modify | Change Health nav from `<span>` to `<a href="/health">` (line 16-19) |
| `src/Aura.UI/Components/Layout/Header.razor` | Modify | Add sign-out button, inject IConfiguration + IHttpContextAccessor + NavigationManager, add HandleSignOut method |

## Interfaces / Contracts

### Health.razor (new page)

```razor
@page "/health"
@using Aura.UI.Components.Dashboard
@using Aura.UI.Components.GraphConnector
@using Aura.UI.Components.Auth
@using Microsoft.AspNetCore.Components.Authorization

<AuthorizeView>
    <Authorized>
        <PageTitle>Aura - Health</PageTitle>
        <div class="dashboard-page-header">
            <div>
                <h1 class="dashboard-page-header__title">System Health</h1>
                <p class="dashboard-page-header__subtitle">Connector status, system indicators, and module progress.</p>
            </div>
        </div>

        <GraphConnectorStatusPanel />
        <SystemStatusPanel />
        <ModuleProgressPanel />
    </Authorized>
    <NotAuthorized>
        <PageTitle>Aura | Acceso Requerido</PageTitle>
        <RestrictedAccessView />
    </NotAuthorized>
</AuthorizeView>
```

### Header.razor sign-out additions

```csharp
// @inject additions
@inject Microsoft.AspNetCore.Http.IHttpContextAccessor HttpContextAccessor
@inject Microsoft.AspNetCore.Components.NavigationManager Navigation
@inject Microsoft.Extensions.Configuration.IConfiguration Configuration

// Button in dashboard-header__actions div
<button class="dashboard-header__icon-btn" @onclick="HandleSignOut" data-testid="sign-out-btn" title="Sign out">
    <span class="material-symbols-outlined">logout</span>
</button>

// Code block addition
@code {
    private bool _useEntraId;

    protected override void OnInitialized()
    {
        _useEntraId = Configuration.GetValue<bool>("UseEntraId");
    }

    private async Task HandleSignOut()
    {
        var httpContext = HttpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var scheme = _useEntraId
                ? Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme
                : Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;

            await httpContext.SignOutAsync(scheme);
        }
        Navigation.NavigateTo("/", forceLoad: true);
    }
}
```

### Sidebar.razor Health nav change

```razor
<!-- Before -->
<span class="dashboard-sidebar__nav-item">
    <span class="material-symbols-outlined dashboard-sidebar__nav-icon">monitor_heart</span>
    Health
</span>

<!-- After -->
<a class="dashboard-sidebar__nav-item" href="/health">
    <span class="material-symbols-outlined dashboard-sidebar__nav-icon">monitor_heart</span>
    Health
</a>
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Health.razor renders under AuthorizeView | bUnit test: render with authenticated state, assert three panels present |
| Unit | Header.razor sign-out calls correct scheme | bUnit test: mock IConfiguration with UseEntraId=true/false, verify SignOutAsync called with correct scheme |
| Unit | PriorityDashboard.razor renders RankedSummaryList before grid | bUnit test: render with connector data, assert DOM order |
| Integration | /health page returns 200 | Add endpoint test following `InitialDashboardEndpointTests` pattern |
| E2E | Sidebar Health link navigates to /health | Extend `DashboardRootBrowserTests` or add new test |
| Architecture | No new dependencies on Infrastructure.Adapters.Dashboard | Existing `DashboardArchitectureTests` covers this — no changes needed |

## Migration / Rollout

No migration required. All changes are additive UI modifications. The Health page is a new route — existing `/` route continues to work. The logout button is additive to the header. Dashboard reorder is a DOM-level change with no data implications.

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `RankedSummaryList` CSS depends on grid position | Low | Layout shift | CSS classes are component-scoped (`ranked-summary-list`), no grid dependency in existing styles |
| Auth mode detection wrong (env mismatch) | Low | Sign-out fails | Reuse exact pattern from `RestrictedAccessView.razor.cs` line 42; both schemes are always registered in Program.cs |
| Missing `@using` for `GraphConnectorStatusPanel` | Low | Compile error | Namespace `Aura.UI.Components.GraphConnector` — verify it exists in _Imports.razor or add explicit using |
| `Sidebar.razor` `<a>` breaks nav-item CSS | Low | Visual regression | Existing nav uses `<a>` for Dashboard (line 10) — same class, same pattern |
| Health page not authorized in prod | Low | 403 on /health | AuthorizeView + RestrictedAccessView matches Index.razor pattern exactly |

## Open Questions

- None — all three changes have clear patterns in the existing codebase to follow.
