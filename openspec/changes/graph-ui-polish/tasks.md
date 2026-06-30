# Tasks: Graph UI Polish

## Review Workload Forecast

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: size-exception
400-line budget risk: Low

| Field | Value |
|-------|-------|
| Estimated changed lines | 120–180 |
| Delivery strategy | exception-ok |

---

## Change 1: Dashboard Reorder

### Task 1: bUnit test — dashboard render order
- **Files**: `tests/Aura.UnitTests/Dashboard/PriorityDashboardRenderOrderTests.cs`
- **Test**: Render `PriorityDashboard`. Assert `RankedSummaryList` before `connector-cards`.
- **Verify**: Test fails (currently reversed).
- **Estimate**: 20 min

### Task 2: Reorder RankedSummaryList
- **Files**: `src/Aura.UI/Components/Dashboard/PriorityDashboard.razor`
- **Implement**: Move `<RankedSummaryList>` before `connector-cards-grid`.
- **Verify**: Task 1 GREEN. `dotnet build` + `dotnet test` pass.
- **Estimate**: 5 min

### Task 3: Remove health panels from dashboard
- **Files**: `src/Aura.UI/Components/Dashboard/PriorityDashboard.razor`
- **Test**: bUnit test asserting three panels NOT rendered.
- **Implement**: Delete `<GraphConnectorStatusPanel>`, `<SystemStatusPanel>`, `<ModuleProgressPanel>`.
- **Verify**: New test GREEN. `dotnet build` succeeds.
- **Estimate**: 15 min

---

## Change 2: Health Page

### Task 4: bUnit test — Health.razor renders panels
- **Files**: `tests/Aura.UnitTests/Dashboard/HealthPageTests.cs`
- **Test**: Render `Health` with auth. Assert three panels present.
- **Verify**: Test fails (page doesn't exist).
- **Estimate**: 20 min

### Task 5: Create Health.razor
- **Files**: `src/Aura.UI/Pages/Health.razor`
- **Implement**: `@page "/health"`, `AuthorizeView`, three panels, `RestrictedAccessView` for not-authorized.
- **Verify**: Task 4 GREEN. `dotnet build` succeeds.
- **Estimate**: 15 min

### Task 6: bUnit test — sidebar health link
- **Files**: `tests/Aura.UnitTests/Dashboard/SidebarNavigationTests.cs`
- **Test**: Render `Sidebar`. Assert `a[href="/health"]` exists.
- **Verify**: Test fails (currently `<span>`).
- **Estimate**: 15 min

### Task 7: Update Sidebar Health to anchor
- **Files**: `src/Aura.UI/Components/Layout/Sidebar.razor`
- **Implement**: Change `<span>` to `<a href="/health">`.
- **Verify**: Task 6 GREEN. `dotnet build` succeeds.
- **Estimate**: 5 min

### Task 8: Integration test — /health endpoint
- **Files**: `tests/Aura.IntegrationTests/Dashboard/HealthPageEndpointTests.cs`
- **Test**: `/health` returns 200 with token, 401 without (per `InitialDashboardEndpointTests` pattern).
- **Verify**: Test fails (route doesn't exist).
- **Estimate**: 20 min

---

## Change 3: Logout Button

### Task 9: bUnit test — Header sign-out button
- **Files**: `tests/Aura.UnitTests/Dashboard/HeaderSignOutTests.cs`
- **Test**: Render `Header` with `UseEntraId=true`. Assert `sign-out-btn` exists. Click → `SignOutAsync` with OIDC scheme.
- **Verify**: Test fails (button doesn't exist).
- **Estimate**: 25 min

### Task 10: Add sign-out to Header.razor
- **Files**: `src/Aura.UI/Components/Layout/Header.razor`
- **Implement**: Inject `IHttpContextAccessor`, `NavigationManager`, `IConfiguration`. Add `HandleSignOut` with scheme branching. Add logout button.
- **Verify**: Task 9 GREEN. Both OIDC and cookie paths. `dotnet build` succeeds.
- **Estimate**: 25 min

---

## Phase 4: Verification

### Task 11: Full build and test suite
- **Test**: `dotnet build Aura.sln` + `dotnet test Aura.sln`.
- **Verify**: Zero errors, zero failures.
- **Estimate**: 10 min

### Task 12: E2E sidebar health navigation
- **Files**: `tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs`
- **Test**: Navigate `/test-dashboard`, click Health → `/health` loads with panels.
- **Verify**: Playwright headless passes.
- **Estimate**: 20 min

---

## Summary

| Metric | Value |
|--------|-------|
| Tasks | 12 |
| Estimated time | ~3.5h |
| Critical path | 1→2→3, 4→5→8, 6→7, 9→10, 11→12 |
| Parallelizable | Changes 1, 2, 3 independent |
