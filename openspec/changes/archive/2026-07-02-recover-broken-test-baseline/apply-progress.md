# Apply Progress: recover-broken-test-baseline

## Mode

- Strict TDD: **Active**
- Delivery strategy: **exception-ok**
- PR boundary: **single change with maintainer-approved size:exception**

## Task Completion

- [x] 1.1 RED — Confirmed failing REQ-01 coverage in `AuthorizationFlowTests` before fix
- [x] 1.2 GREEN — Pinned `UseEntraId=false` in integration auth test factory
- [x] 1.3 VERIFY — `dotnet test tests/Aura.IntegrationTests --filter "AuthorizationFlowTests"` passes
- [x] 2.1 RED — Dashboard smoke tests failed on authorized-shell selectors (login gate rendered)
- [x] 2.2 GREEN — Added shared authenticated test `AuthenticationStateProvider`
- [x] 2.3 GREEN — Wired authenticated test user into `InitialDashboardSmokeTests`
- [x] 2.4 GREEN — Wired authenticated test user into inbox/sync/graph smoke suites
- [x] 2.5 REFACTOR — Consolidated repeated auth test wiring via extension helper
- [x] 3.1 RED — Playwright bootstrap failed against hardcoded localhost host
- [x] 3.2 GREEN — Registered auth services/test auth provider in Playwright self-host factory
- [x] 3.3 GREEN — Added host reachability probe with explicit `HostNotReachable` failure
- [x] 3.4 GREEN — Migrated Playwright bootstrap tests to `PlaywrightWebApplicationFactory`
- [x] 4.1 VERIFY — `dotnet test tests/Aura.E2E --filter "FullyQualifiedName~Dashboard|FullyQualifiedName~Playwright"` passes
- [x] 4.2 VERIFY — `dotnet test Aura.sln` full green baseline (architecture blocker resolved)
- [x] 4.3 CLASSIFY — Refactor fallout classified and resolved (regression fixes in test host/wiring + intentional selector alignment)
- [x] 5.1 Updated guardrail wording for REQ-03 host-gate diagnostics
- [x] 5.2 Updated stable selector inventory with sync selector deprecation note
- [x] 5.3 Updated Playwright bootstrap spec with selector contract cross-reference and host gate
- [x] 3.3 (continuation) Added runtime test coverage proving `HostNotReachable` named failure for non-success health probe and transport exception paths
- [x] Verify-gap metadata sync (continuation) Updated `openspec/config.yaml` E2E availability to `true` with Playwright tooling evidence

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.IntegrationTests/Auth/AuthorizationFlowTests.cs` | Integration | ✅ 3/4 passing baseline (known failing target test) | ✅ Existing failing scenario confirmed | ✅ Added `UseEntraId=false` + tests pass | ✅ Added explicit login/token assertions | ➖ None needed |
| 2.1 | `tests/Aura.E2E/Dashboard/InitialDashboardSmokeTests.cs` | E2E (host-level) | ✅ Existing suite executed and failures captured | ✅ Missing authorized markers captured | ✅ Auth provider wiring restored markers | ✅ Applied to multiple dashboard smoke suites | ✅ Extracted shared helper |
| 3.1 | `tests/Aura.E2E/Playwright/PlaywrightBootstrapTests.cs` | E2E (browser) | ✅ Existing Playwright bootstrap failure captured | ✅ Hardcoded host failure reproduced | ✅ Self-host factory migration passes | ✅ Added host probe failure diagnostics path | ✅ Shared startup/auth wiring centralized |
| 4.1 | `tests/Aura.E2E/**/*Dashboard*,*Playwright*` | E2E | ✅ 0/29 baseline before fixes | ✅ Failure set captured earlier | ✅ 29/29 passing | ➖ Covered by prior slices | ➖ None needed |
| 4.2 | `Aura.sln` | Full suite | ✅ Focused safety net + full-suite baseline executed | ✅ Architecture dependency failure reproduced first | ✅ Application DTO boundary fix + full suite green | ✅ Covered by targeted use-case/UI + architecture checks | ✅ Removed API→Domain dependency while preserving payload shape |
| 5.1-5.3 | `openspec/specs/*` | Spec docs | N/A (doc updates) | ✅ Requirements validated against failing modes | ✅ Specs updated to reflect implemented behavior | ➖ Single documentation path | ✅ Clarified guardrail wording |
| 3.3 (cont.) | `tests/Aura.E2E/Browser/PlaywrightHostReachabilityGateTests.cs` + `tests/Aura.E2E/Browser/PlaywrightWebApplicationFactory.cs` | E2E (host gate runtime) | ✅ Existing Playwright/browser suites passing before change | ✅ Written first: failing-probe runtime assertions for `503` and transport exception paths | ✅ Passed: named `HostNotReachable` with URL+port and inner-exception evidence | ✅ Added success-path no-throw case to avoid false positives | ✅ Small seam (`HttpMessageHandler` injection) to runtime-test the probe without changing production behavior |
| Config sync (cont.) | `openspec/config.yaml` | Config docs | N/A (metadata update) | ✅ Verify warning validated | ✅ E2E availability marked true with Playwright tooling note | ➖ Single documentation path | ✅ Keeps planning metadata aligned with implemented test capabilities |

## Test Execution Log

- ✅ `dotnet test tests/Aura.IntegrationTests --filter "AuthorizationFlowTests"`
- ✅ `dotnet test tests/Aura.E2E --filter "FullyQualifiedName~InitialDashboardSmokeTests"`
- ✅ `dotnet test tests/Aura.E2E --filter "FullyQualifiedName~InboxPreviewPanelFieldsSmokeTests|FullyQualifiedName~SyncStatusPanelSmokeTests"`
- ✅ `dotnet test tests/Aura.E2E --filter "FullyQualifiedName~PlaywrightBootstrapTests"`
- ✅ `dotnet test tests/Aura.E2E --filter "FullyQualifiedName~Dashboard|FullyQualifiedName~Playwright"`
- ✅ `dotnet test tests/Aura.IntegrationTests --filter "FullyQualifiedName~Dashboard|FullyQualifiedName~GraphConnector|FullyQualifiedName~Sync|FullyQualifiedName~CorsMockLoginTests|FullyQualifiedName~AuthorizationFlowTests"`
- ✅ `dotnet test tests/Aura.E2E --filter "FullyQualifiedName~GraphConnectorStatusSmokeTests"`
- ✅ `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~GetUpcomingMeetingsUseCaseTests|FullyQualifiedName~UpcomingMeetingsPanelTests"`
- ✅ `dotnet test tests/Aura.ArchitectureTests/Aura.ArchitectureTests.csproj --filter "FullyQualifiedName~DashboardEndpointTypes_ShouldNotReference_AuraDomain"`
- ✅ `dotnet test Aura.sln` (all projects green)
- ✅ `dotnet test tests/Aura.E2E/Aura.E2E.csproj --filter "FullyQualifiedName~PlaywrightHostReachabilityGateTests" -v minimal`
- ✅ `dotnet test tests/Aura.E2E/Aura.E2E.csproj --filter "FullyQualifiedName~PlaywrightBootstrapTests|FullyQualifiedName~DashboardRootBrowserTests|FullyQualifiedName~HealthRouteBrowserTests|FullyQualifiedName~PlaywrightHostReachabilityGateTests" -v minimal`
- ✅ `dotnet test Aura.sln -v minimal` (regression safety rerun after host-gate runtime tests)

## Deviation / Notes

- No production auth/layout rollback was performed.
- Regression fixes were applied where failures indicated broken host/auth wiring.
- Intentional selector contract was enforced by aligning component marker (`sync-source-progress-*`) and smoke tests.
- Upcoming meetings payload boundary moved to `Aura.Application.Models.UpcomingMeetingDto`; API no longer maps `Aura.Domain.Calendar` types directly.
- `size:exception` was used as approved by maintainer.

## Continuation Batch — Verify Gap Closure

- Scope held to verify blocker only: runtime proof for REQ-03 `HostNotReachable` plus low-risk OpenSpec testing-capability metadata sync.
- No production feature behavior changed; probe logic behavior remains unchanged.
- Added runtime negative-path tests to convert source-only evidence into executable proof.
