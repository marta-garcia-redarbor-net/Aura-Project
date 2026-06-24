# Spec: playwright-e2e-bootstrap

**Change**: playwright-e2e-bootstrap  
**Phase**: spec  
**Date**: 2026-06-24

---

## Requirements

### Functional Requirements

| ID | Requirement | Acceptance Criteria |
|---|---|---|
| FR-1 | Playwright packages installed and configured | `dotnet test tests/Aura.E2E` discovers and runs Playwright tests alongside xUnit |
| FR-2 | Browser test for dashboard root renders shell | Test opens `/`, waits for `data-testid="dashboard-shell"` visible, asserts presence |
| FR-3 | Browser test validates loading transition | Test detects loading state marker, then polls for settled/populated state, validates transition completes |
| FR-4 | Failure artifacts captured | On test failure: screenshot and trace file written to `tests/Aura.E2E/TestResults/` |
| FR-5 | Controlled data seeding | Test uses mocked/stubbed backend responses; no external deps |
| FR-6 | Test is deterministic | Test passes consistently; no random waits or time-based flakes |
| FR-7 | Existing smoke tests unchanged | All host-level smoke tests in `tests/Aura.E2E` continue to pass unchanged |

### Non-Functional Requirements

| ID | Requirement | Acceptance Criteria |
|---|---|---|
| NFR-1 | Test execution time | Browser test completes in < 10 seconds locally |
| NFR-2 | Selector stability | All assertions use stable `data-testid` or role-based queries, not text/CSS fragile selectors |
| NFR-3 | Documentation | Readme or inline comment explains how to run browser tests vs smoke tests |
| NFR-4 | Configuration simplicity | Playwright config is minimal; no CI integration yet |

---

## Scenarios

### Scenario 1: Dashboard Root Shell Visibility

**Given** the browser test launches the app on `http://localhost:5000` (or configured URL)  
**When** the page loads with controlled dashboard data (mocked empty or populated state)  
**Then**  
- The dashboard shell renders (marker: `data-testid="dashboard-shell"`)
- No console errors logged during render
- Test passes and completes within timeout

**Data**: Stub response with empty card list or 1-2 sample cards

---

### Scenario 2: Loading State Transition

**Given** the app is configured to delay the dashboard API response by 100-200ms  
**When** the browser test loads `/`  
**Then**  
- Initial render shows loading state (marker: `data-testid="dashboard-state-loading"`)
- After API response settles, loading marker disappears
- Populated or empty state marker appears (e.g., `data-testid="dashboard-state-populated"` or `data-testid="dashboard-state-empty"`)
- Test asserts both markers were present in correct order

**Data**: Delayed stub response; known delay ensures predictability

---

### Scenario 3: Failure Artifact Capture

**Given** a test fails (e.g., shell marker never appears, or state transition times out)  
**When** the test assertion fails  
**Then**  
- Screenshot written to `tests/Aura.E2E/TestResults/failure-screenshot-{timestamp}.png`
- Playwright trace written to `tests/Aura.E2E/TestResults/trace-{timestamp}.zip`
- Console logs captured in trace for debugging

---

## Test Structure

### File Location
```
tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs
```

### Test Class Pattern
```csharp
public class DashboardRootBrowserTests : IAsyncLifetime
{
    private IBrowser _browser;
    private IPage _page;
    private readonly ITestOutputHelper _output;

    [Fact]
    public async Task DashboardRoot_WithControlledData_RenderShellAndLoadingTransition()
    {
        // Arrange: configure stub backend, launch browser, navigate
        // Act: wait for shell and state markers
        // Assert: markers present, transition occurred, no console errors
    }
}
```

### Data Setup
- Use `WebApplicationFactory<UiMarker>` or similar test host to wire controlled responses
- Replace real `IDashboardApiClient` with stub returning controlled data
- Ensure deterministic delays (e.g., `DelayedDashboardApiClient` already exists in smoke tests)

---

## Boundaries

### Included
- Playwright browser automation
- xUnit test framework integration
- Local dev Playwright config (headless or headed mode selectable)
- Failure artifacts (screenshot/trace)
- Stable `data-testid` selectors

### Not Included
- CI/CD integration (defer to W4-H2)
- Real backend API calls
- Navigation between multiple pages
- Teams, Outlook, Calendar connector tests
- Performance benchmarking
- Cross-browser testing (Chrome only for MVP)

---

## Selectors & Markers (from existing smoke tests)

These markers already exist in the codebase and are stable:
- `data-testid="dashboard-shell"`
- `data-testid="dashboard-sidebar"`
- `data-testid="dashboard-header"`
- `data-testid="dashboard-main"`
- `data-testid="dashboard-state-loading"`
- `data-testid="dashboard-state-populated"`
- `data-testid="dashboard-state-empty"`
- `data-testid="dashboard-state-error"`

**No new selectors need to be added to production code.**

---

## Constraints & Assumptions

| Constraint | Rationale |
|---|---|
| Controlled data only | Determinism + no flakiness from external integrations |
| Root route only | Minimal slice; multi-page E2E deferred |
| No CI yet | Playwright integration can run locally; CI added in W4-H2 |
| xUnit inside existing project | Reuse `tests/Aura.E2E.csproj` structure; no new test project |
| <10s execution time | Browser test must remain fast feedback loop, not slow check |

---

## Open Questions for Design/Tasks Phase

1. Should the test fixture use `WebApplicationFactory<UiMarker>` (like smoke tests) or launch the UI separately and stub HTTP only?
2. What is the exact controlled dashboard response payload (empty, 1 card, 2 cards)?
3. Should trace capture be always-on or only-on-failure?
4. How to handle browser type selection (Chromium, Firefox, WebKit)? Start with Chromium only?

---

## Related Standards

- Existing smoke tests: `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` — use similar data flow
- Clean Arch guard: browser tests stay at E2E boundary; no domain logic testing here
- Playwright best practices: stable locators, explicit waits, failure artifacts
