# Tasks: playwright-e2e-bootstrap

**Change**: playwright-e2e-bootstrap  
**Phase**: tasks  
**Date**: 2026-06-24  
**Delivery strategy**: single-pr-default  
**Review budget**: 2000 lines  
**Execution mode**: auto

---

## Task Breakdown

### Phase 1: Setup & Dependencies

#### Task 1.1: Add Playwright NuGet to Aura.E2E.csproj
- **Description**: Install Microsoft.Playwright package and required dependencies
- **Files affected**: `tests/Aura.E2E/Aura.E2E.csproj`
- **Changes**:
  - Add `<PackageReference Include="Microsoft.Playwright" Version="1.50.0" />`
  - Restore packages: `dotnet restore tests/Aura.E2E`
- **Verification**: `dotnet test tests/Aura.E2E --list-tests` shows Playwright tests discoverable
- **Effort**: Low (15 min)
- **DoD**: Packages installed, no restore errors, test discovery works

#### Task 1.2: Create Browser folder structure
- **Description**: Create folder and placeholder for browser tests
- **Files affected**: Create `tests/Aura.E2E/Browser/` (new folder)
- **Changes**:
  - Create `tests/Aura.E2E/Browser/` directory
  - Create `tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs` (skeleton)
- **Verification**: Folder structure matches design
- **Effort**: Trivial (5 min)
- **DoD**: Folders exist, compile succeeds

---

### Phase 2: Test Infrastructure

#### Task 2.1: Implement DashboardRootBrowserTests fixture setup
- **Description**: Wire `WebApplicationFactory` fixture + browser lifecycle
- **Files affected**: `tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs`
- **Changes**:
  - Create test class extending `IAsyncLifetime`
  - Implement `InitializeAsync()` to launch Playwright Chromium browser
  - Implement `DisposeAsync()` to close browser gracefully
  - Wire constructor with `ITestOutputHelper` + `WebApplicationFactory<UiMarker>`
  - Create `CreateClient()` helper method to inject stub API clients
- **Verification**: 
  - Compile succeeds
  - Browser launches/closes without error (manual smoke test)
- **Effort**: Medium (45 min)
- **DoD**: 
  - Fixture initializes and disposes cleanly
  - Test host can be created with stubs
  - Compile succeeds

#### Task 2.2: Implement controlled data stub injection
- **Description**: Configure test to use delayed/controlled dashboard responses
- **Files affected**: `tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs`
- **Changes**:
  - Reuse `DelayedDashboardApiClient` from smoke tests
  - In test setup, configure stub with 150ms delay
  - Configure stub with sample dashboard response (1-2 cards)
  - Wire stub into `ConfigureTestServices()`
- **Verification**: 
  - Stub correctly configured in test
  - Delay respected in manual test
- **Effort**: Low (20 min)
- **DoD**: 
  - Stub is injected correctly
  - Test host receives controlled response
  - Delay is observable

---

### Phase 3: Browser Test Logic

#### Task 3.1: Implement browser navigation and shell visibility assertion
- **Description**: Test navigates to `/` and validates shell renders
- **Files affected**: `tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs`
- **Changes**:
  - In test method, navigate to `http://localhost:5000/` via `page.GotoAsync()`
  - Wait for `data-testid="dashboard-shell"` to be visible
  - Assert shell marker is present in page HTML/DOM
  - Assert no console errors logged
- **Verification**: 
  - Test passes locally
  - Shell marker detected
  - Console clean
- **Effort**: Medium (30 min)
- **DoD**: 
  - Navigation succeeds
  - Shell marker found
  - No console errors
  - Test assertion passes

#### Task 3.2: Implement loading state detection and transition validation
- **Description**: Test detects loading marker, then validates transition to populated/empty state
- **Files affected**: `tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs`
- **Changes**:
  - After navigation, wait for `data-testid="dashboard-state-loading"` to appear
  - Wait for loading marker to disappear (with timeout)
  - Assert that `data-testid="dashboard-state-populated"` OR `data-testid="dashboard-state-empty"` appears after loading disappears
  - Verify state transition occurs within expected time (~200ms)
- **Verification**: 
  - Loading state appears
  - Transition detected
  - Populated or empty state replaces loading
- **Effort**: Medium (35 min)
- **DoD**: 
  - Both state markers detected in correct order
  - Transition timing reasonable (<2s total)
  - Test assertion passes

#### Task 3.3: Add failure artifact capture
- **Description**: On test failure, capture screenshot and trace for debugging
- **Files affected**: `tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs`
- **Changes**:
  - Enable Playwright trace recording at browser context creation
  - Add test cleanup to dump trace file on failure
  - Add `page.ScreenshotAsync()` call on test failure (or via fixture)
  - Configure output path: `tests/Aura.E2E/TestResults/`
- **Verification**: 
  - Manually trigger test failure
  - Confirm screenshot and trace files created
  - Files are readable/debuggable
- **Effort**: Low (20 min)
- **DoD**: 
  - Trace recording enabled
  - Screenshot captured on failure
  - Artifacts stored in correct folder
  - At least 1 manual failure test confirms artifacts exist

---

### Phase 4: Validation & Existing Test Preservation

#### Task 4.1: Verify all existing smoke tests still pass
- **Description**: Ensure host-level tests in Dashboard/, Auth/, GraphConnector/ remain unchanged and passing
- **Files affected**: None (verification only)
- **Changes**: None
- **Verification**: 
  - Run `dotnet test tests/Aura.E2E` (full suite)
  - All existing test classes pass
  - No regressions
- **Effort**: Low (15 min)
- **DoD**: 
  - All existing tests passing
  - No new failures introduced
  - Coverage report shows no regressions

#### Task 4.2: Validate browser test passes consistently (5 runs)
- **Description**: Confirm new browser test is not flaky
- **Files affected**: None (verification only)
- **Changes**: None
- **Verification**: 
  - Run `dotnet test tests/Aura.E2E/Browser/DashboardRootBrowserTests` 5 times locally
  - All 5 runs pass
  - No intermittent failures
- **Effort**: Low (10 min, mostly wait time)
- **DoD**: 
  - 5/5 runs pass
  - No flakiness detected
  - Test is stable

---

### Phase 5: Documentation

#### Task 5.1: Document how to run browser tests vs smoke tests
- **Description**: Add README or inline doc explaining test categories
- **Files affected**: `tests/Aura.E2E/README.md` (create or update)
- **Changes**:
  - Create or update `tests/Aura.E2E/README.md`
  - Document how to run all tests: `dotnet test tests/Aura.E2E`
  - Document how to run smoke tests only: `dotnet test tests/Aura.E2E --filter "Category!=Browser"`
  - Document how to run browser tests only: `dotnet test tests/Aura.E2E/Browser`
  - Note: Playwright only runs locally; not in CI yet
  - Note: Browser tests require X11 or headless support (if running on Linux CI later)
- **Verification**: 
  - README is clear and accurate
  - Commands work as documented
- **Effort**: Low (15 min)
- **DoD**: 
  - README exists and is up to date
  - Commands documented and tested

#### Task 5.2: Add inline code comments explaining test structure
- **Description**: Comment key test methods for maintainability
- **Files affected**: `tests/Aura.E2E/Browser/DashboardRootBrowserTests.cs`
- **Changes**:
  - Add XML doc comments to test class
  - Add comments to `InitializeAsync()` and `DisposeAsync()` explaining browser lifecycle
  - Add comments to test method explaining each assertion step
- **Verification**: 
  - Comments are clear and helpful
  - No ambiguity about what test validates
- **Effort**: Trivial (10 min)
- **DoD**: 
  - Comments added
  - Comments are accurate and helpful

---

## Summary

| Task | Phase | Effort | DoD Status |
|------|-------|--------|-----------|
| 1.1 Add Playwright NuGet | 1 | Low | Done |
| 1.2 Create Browser folder | 1 | Trivial | Done |
| 2.1 Fixture setup | 2 | Medium | Done |
| 2.2 Stub injection | 2 | Low | Done |
| 3.1 Shell visibility | 3 | Medium | Done |
| 3.2 Loading transition | 3 | Medium | Done |
| 3.3 Artifact capture | 3 | Low | Done |
| 4.1 Verify smoke tests | 4 | Low | Done |
| 4.2 Flakiness check (5x) | 4 | Low | Done |
| 5.1 README | 5 | Low | Done |
| 5.2 Code comments | 5 | Trivial | Done |

**Total effort**: ~4-5 hours (realistic for 1-2 developers)  
**Estimated lines changed**: ~300-400 (mostly test code, minimal app changes)

---

## Work Unit Commits (Recommended)

When implementing, break into reviewable commits:

```
Commit 1: Add Playwright NuGet + folder structure
Commit 2: Implement test fixture and browser lifecycle
Commit 3: Add shell visibility assertion
Commit 4: Add loading state transition validation
Commit 5: Add failure artifact capture
Commit 6: Add documentation and inline comments
```

**Each commit should be independently testable and reviewable.**

---

## Blockers & Dependencies

| Blocker | Mitigation | Status |
|---------|-----------|--------|
| Playwright version conflict with xUnit | Pin version; verify locally | None expected |
| Browser launch fails in test env | May need X11 or headless config; defer CI to W4-H2 | None expected |
| Stub data doesn't match current API | Use same stubs as smoke tests; validate contract | None expected |

---

## Review Workload Forecast

- **Total changed lines**: ~350-450 (mostly new test code)
- **Risk level**: Low (isolated to test folder, no app changes)
- **Review complexity**: Low (follows existing smoke test pattern)
- **Chained PR recommendation**: No
- **Single PR suitable**: Yes
- **Decision needed before apply**: No

**Ready to implement as one PR to `feat/playwright-e2e-bootstrap`.**

---

## Next Steps (After Tasks)

1. **Apply phase**: Execute tasks in order, verify each step
2. **Verify phase**: Run full test suite, capture evidence
3. **Archive phase**: Close change, document learnings, prepare for W4-H2
