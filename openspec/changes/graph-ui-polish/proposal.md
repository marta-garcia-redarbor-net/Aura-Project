# Proposal: Graph UI Polish

## Intent

The Priority Dashboard is live at `/` with connector cards, sync button, and ranked summary list. Three UI adjustments are needed: reorder dashboard content so the ranked summary appears first, extract health/status panels into a dedicated `/health` page, and add a logout button in the header. All three are pure UI changes — no backend, no new DI, no new API endpoints.

## Scope

### In Scope

1. **Dashboard reorder**: Move `<RankedSummaryList>` before the connector cards grid in `PriorityDashboard.razor`. SyncButton stays with connector cards.
2. **Health page at `/health`**: Create `Pages/Health.razor` with `@page "/health"` containing `GraphConnectorStatusPanel`, `SystemStatusPanel`, and `ModuleProgressPanel`. Update `Sidebar.razor` so the Health nav item links to `/health`.
3. **Logout button**: Add sign-out button to `Header.razor` using `IHttpContextAccessor` + `NavigationManager`. OIDC mode calls `SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme)`, dev mode calls `SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)`. Redirect to `/` after sign-out.

### Out of Scope

- New backend endpoints or DI registrations (all API clients already registered)
- New capabilities beyond the three UI adjustments
- Navigation/routing changes beyond the Health sidebar link
- Mobile responsiveness or layout redesign

## Capabilities

### New Capabilities
- `health-page`: Dedicated health/status page at `/health` with Graph connector, system status, and module progress panels

### Modified Capabilities
- `initial-dashboard`: Dashboard reorder moves RankedSummaryList before connector cards; health/status panels removed from dashboard (moved to health page)

## Approach

1. **Change 1 (reorder)**: Swap markup positions in `PriorityDashboard.razor`. `RankedSummaryList` moves before the `connector-cards-grid` div. No logic changes.
2. **Change 2 (health page)**: Create `src/Aura.UI/Pages/Health.razor` following `Index.razor` pattern (AuthorizeView wrapper). Move three panels from dashboard. Update `Sidebar.razor` Health nav from `<span>` to `<a href="/health">`.
3. **Change 3 (logout)**: Inject `IHttpContextAccessor` and `NavigationManager` into `Header.razor`. Detect auth mode via config/feature flag. Add sign-out button with redirect.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.UI/Components/Dashboard/PriorityDashboard.razor` | Modified | Reorder RankedSummaryList before connector cards |
| `src/Aura.UI/Pages/Health.razor` | New | Health page with status panels |
| `src/Aura.UI/Components/Layout/Sidebar.razor` | Modified | Health nav item becomes link |
| `src/Aura.UI/Components/Layout/Header.razor` | Modified | Logout button added |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Auth mode detection incorrect | Low | Use existing `AuthenticationStateProvider` pattern from project |
| Dashboard reorder breaks layout CSS | Low | CSS classes are component-scoped, no grid dependency |
| Health page missing authorization | Low | Follow same AuthorizeView pattern as Index.razor |

## Rollback Plan

Revert the four files via git. All changes are isolated to UI layer — no database, no backend, no config changes to undo.

## Dependencies

None. All API clients and auth infrastructure already exist.

## Success Criteria

- [ ] RankedSummaryList renders before connector cards on `/`
- [ ] `/health` page loads with GraphConnectorStatusPanel, SystemStatusPanel, ModuleProgressPanel
- [ ] Sidebar Health nav item navigates to `/health`
- [ ] Header logout button signs out and redirects to `/`
- [ ] `dotnet build Aura.sln` succeeds
- [ ] `dotnet test Aura.sln` passes
