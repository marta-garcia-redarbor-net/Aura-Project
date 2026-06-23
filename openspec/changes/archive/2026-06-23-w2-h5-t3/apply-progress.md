# Apply Progress: W2-H5-T3 Morning Summary Timezone Scheduling

## Change
- **Name**: `w2-h5-t3`
- **Mode**: Strict TDD
- **Artifact store**: OpenSpec
- **Workload decision**: `single-pr` with maintainer-approved `size:exception` (explicitly accepted)

## Completed Tasks

- [x] 1.1 Create Application ports for settings, scheduler, and emission store.
- [x] 1.2 Create Application models for settings and due-state.
- [x] 1.3 RED tests for scheduler settings chain / DST wall-clock / due-state.
- [x] 1.4 RED tests for SQLite emission guard duplicate/next-day/restart/reset behavior.
- [x] 2.1 Implement scheduler with configured -> system -> UTC timezone chain and guard lookup.
- [x] 2.2 Implement config options + provider adapter with `TargetLocalTime` default `09:00`.
- [x] 2.3 Implement persisted SQLite emission guard on shared `aura.db`.
- [x] 2.4 Register Morning Summary scheduling adapters in Infrastructure DI.
- [x] 3.1 Register scheduler use case in Application DI.
- [x] 3.2 Wire scheduling adapters in Infrastructure root DI.
- [x] 3.3 Add worker with fixed single-operator system user identity and source-generated logs.
- [x] 3.4 Wire worker host + appsettings (`MorningSummary` + `ConnectionStrings:Aura`).
- [x] 4.1 Make scheduler tests GREEN for fallback chain, DST wall-clock, due-state, same-day block.
- [x] 4.2 Make SQLite store tests GREEN for mark/duplicate/reset/restart behavior.
- [x] 4.3 Add/update DI + worker wiring tests.
- [x] 4.4 Run `dotnet test Aura.sln` and keep scope limited to scheduling/timezone/idempotence.
- [x] 5.1 Refactor timezone/date conversion duplication (`FormatLocalDate`) and keep scheduler read-only.
- [x] 5.2 Keep override seam programmatic-only (`ResetAsync`) and out of UI scope.

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.UnitTests/MorningSummary/MorningSummarySchedulerTests.cs` | Unit | N/A (new) | ✅ Compile-fail RED from missing ports | ✅ Scheduler tests compile/pass after ports | ➖ Structural (interfaces only) | ➖ None needed |
| 1.2 | `tests/Aura.UnitTests/MorningSummary/MorningSummarySchedulerTests.cs` | Unit | N/A (new) | ✅ Compile-fail RED from missing models | ✅ Scheduler tests compile/pass after models | ➖ Structural (records only) | ➖ None needed |
| 1.3 | `tests/Aura.UnitTests/MorningSummary/MorningSummarySchedulerTests.cs` | Unit | N/A (new) | ✅ Written first (initial run failed) | ✅ Passed (6 scheduler assertions/cases) | ✅ 6 cases (configured/system/UTC fallback, DST, before-due, duplicate) | ✅ Kept scheduler pure/read-only |
| 1.4 | `tests/Aura.UnitTests/MorningSummary/SqliteMorningSummaryEmissionStoreTests.cs` | Unit | N/A (new) | ✅ Written first (initial run failed) | ✅ Passed (5 store assertions/cases) | ✅ 5 cases (mark/duplicate/next-day/reset/restart) | ✅ Added disposable cleanup guard |
| 2.1 | `tests/Aura.UnitTests/MorningSummary/MorningSummarySchedulerTests.cs` | Unit | N/A (new files) | ✅ Existing RED from missing implementation | ✅ Scheduler implementation makes tests pass | ✅ Fallback and due/not-due branches covered | ✅ No state mutation in scheduler |
| 2.2 | `tests/Aura.UnitTests/MorningSummary/MorningSummarySchedulerTests.cs` | Unit | N/A (new files) | ✅ RED from unresolved provider/options wiring | ✅ Provider/options implementation passes scheduler path | ✅ Config parse + default path exercised | ✅ Kept provider parsing minimal |
| 2.3 | `tests/Aura.UnitTests/MorningSummary/SqliteMorningSummaryEmissionStoreTests.cs` | Unit | N/A (new files) | ✅ RED from missing SQLite store | ✅ Store implementation passes all guard tests | ✅ Duplicate + reset + restart + next-day covered | ✅ Extracted local-date formatter |
| 2.4 | `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` | Unit | ✅ 15/15 targeted baseline | ✅ Added failing resolution expectation first | ✅ Adapter DI registration resolves provider/store | ✅ Resolution + options/connection config paths | ✅ Minimal adapter DI surface |
| 3.1 | `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs` | Unit | ✅ 15/15 targeted baseline | ✅ Added resolution test first | ✅ `IMorningSummaryScheduler` resolves and executes | ✅ Scheduler resolution + behavior assertion | ✅ No extra DI coupling |
| 3.2 | `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` | Unit | ✅ 15/15 targeted baseline | ✅ Failing test before root DI wiring | ✅ Root DI now exposes scheduling adapters | ✅ Existing infra registrations still resolve | ✅ Kept additive registration only |
| 3.3 | `tests/Aura.UnitTests/Workers/MorningSummarySchedulingWorkerTests.cs` | Unit | N/A (new file) | ✅ Worker tests written first | ✅ Worker marks emission only when due | ✅ Due + not-due branches with fixed `system` identity | ✅ Programmatic iteration seam (`ProcessIterationAsync`) |
| 3.4 | `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs` | Integration | ✅ 4/4 focused baseline | ✅ Composition test would fail without config wiring | ✅ Host composition remains GREEN with new config | ✅ Composition tested with and without scheduling dependencies | ✅ Kept config additive |
| 4.1 | `tests/Aura.UnitTests/MorningSummary/MorningSummarySchedulerTests.cs` | Unit | N/A (new) | ✅ Already RED before implementation | ✅ GREEN confirmed in focused and full suite runs | ✅ Multiple timezone and due-state paths | ✅ No behavior change after cleanups |
| 4.2 | `tests/Aura.UnitTests/MorningSummary/SqliteMorningSummaryEmissionStoreTests.cs` | Unit | N/A (new) | ✅ Already RED before implementation | ✅ GREEN confirmed in focused and full suite runs | ✅ Mark/duplicate/reset/restart/next-day | ✅ Date formatting centralized |
| 4.3 | `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs`, `tests/Aura.UnitTests/Workers/MorningSummarySchedulingWorkerTests.cs`, `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs` | Unit | ✅ 15/15 targeted baseline | ✅ Added new assertions first | ✅ All wiring tests pass | ✅ DI + worker branches covered | ✅ No unrelated test rewrites |
| 4.4 | `Aura.sln` full suite | Full suite | ✅ Focused suites passed first | ✅ N/A (execution verification task) | ✅ `dotnet test Aura.sln` passed | ➖ Single verification command | ➖ None needed |
| 5.1 | `tests/Aura.UnitTests/MorningSummary/SqliteMorningSummaryEmissionStoreTests.cs` | Unit | ✅ Focused morning tests green pre-refactor | ✅ Approval-by-existing tests before refactor | ✅ Tests still pass after formatter extraction | ✅ Eliminated duplicate date formatting | ✅ Cleaner implementation |
| 5.2 | `tests/Aura.UnitTests/Workers/MorningSummarySchedulingWorkerTests.cs`, `tests/Aura.UnitTests/MorningSummary/SqliteMorningSummaryEmissionStoreTests.cs` | Unit | ✅ Focused tests green pre-guard | ✅ Existing tests enforce seam boundaries | ✅ Reset remains programmatic and worker remains UI-free | ✅ Due/not-due + reset behaviors protect scope | ✅ Scope guard preserved |

## Test Summary

- **Total tests written (new)**: 13
  - Scheduler tests: 6
  - SQLite emission store tests: 5
  - Worker tests: 2
- **Updated tests**: 3
  - `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs`
  - `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs`
  - `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs`
- **Focused runs**: 15/15 passing (unit), 4/4 passing (integration)
- **Full suite**: `dotnet test Aura.sln` passed (all projects green)
- **Layers used**: Unit + Integration
- **Approval tests (refactor tasks)**: Existing focused tests reused for behavior lock

## Scope Guard Confirmation

- Stayed within scheduling/timezone/idempotence scope.
- No ranking/composition/data-window semantics were implemented.
- Override seam prepared via `IMorningSummaryEmissionStore.ResetAsync(...)` only.
- Worker uses fixed system user id (`system`) for current single-operator mode.

## Files Changed (W2-H5-T3 scope)

### Created
- `src/Aura.Application/Ports/IMorningSummarySettingsProvider.cs`
- `src/Aura.Application/Ports/IMorningSummaryEmissionStore.cs`
- `src/Aura.Application/Ports/IMorningSummaryScheduler.cs`
- `src/Aura.Application/Models/MorningSummarySettings.cs`
- `src/Aura.Application/Models/MorningSummaryDueState.cs`
- `src/Aura.Application/UseCases/MorningSummaryScheduling/MorningSummaryScheduler.cs`
- `src/Aura.Infrastructure/Adapters/MorningSummaryScheduling/MorningSummaryOptions.cs`
- `src/Aura.Infrastructure/Adapters/MorningSummaryScheduling/AppSettingsMorningSummarySettingsProvider.cs`
- `src/Aura.Infrastructure/Adapters/MorningSummaryScheduling/SqliteMorningSummaryEmissionStore.cs`
- `src/Aura.Infrastructure/Adapters/MorningSummaryScheduling/DependencyInjection.cs`
- `src/Aura.Workers/MorningSummarySchedulingWorker.cs`
- `tests/Aura.UnitTests/MorningSummary/MorningSummarySchedulerTests.cs`
- `tests/Aura.UnitTests/MorningSummary/SqliteMorningSummaryEmissionStoreTests.cs`
- `tests/Aura.UnitTests/Workers/MorningSummarySchedulingWorkerTests.cs`

### Modified
- `src/Aura.Application/DependencyInjection.cs`
- `src/Aura.Infrastructure/DependencyInjection.cs`
- `src/Aura.Workers/Program.cs`
- `src/Aura.Workers/appsettings.json`
- `src/Aura.Workers/Aura.Workers.csproj`
- `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs`
- `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs`
- `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs`
- `openspec/changes/w2-h5-t3/tasks.md`

## Remediation Addendum (Post-Verify Gap Closure)

### Verify Gaps Addressed

- ✅ Gap 1 — Added runtime configuration-provider proofs for configured value binding and fallback/default parsing of `targetLocalTime`:
  - `AddAuraInfrastructure_MorningSummarySettingsProvider_BindsConfiguredValues`
  - `AddAuraInfrastructure_MorningSummarySettingsProvider_InvalidTargetLocalTime_FallsBackToDefault`
- ✅ Gap 2 — Replaced pseudo-restart assertion with a true file-backed reopen proof:
  - `HasBeenEmittedAsync_PersistsAcrossRepositoryRestart_OnSameDatabase`
  - Uses temp SQLite file + first connection dispose + second connection reopen.
- ✅ Gap 3 — Added scheduler-level proof that next local day becomes due after prior-day emission:
  - `Scheduler_WithPersistedEmission_NextDayAtOrAfterTarget_IsDueAgain`
- ✅ Gap 4 — Added scheduler/store proof that reset enables forced re-emission:
  - `Scheduler_AfterResetForSameDay_AllowsForcedReEmission`
- ✅ Gap 5 — Added hosted-service composition assertion for worker registration in `IHostedService`:
  - `WorkerHost_RegistersMorningSummarySchedulingWorker_AsHostedService`

### Remediation TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| R1 | `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` | Unit | ✅ 22/22 targeted baseline | ✅ Added failing config-provider binding/fallback assertions first | ✅ Both provider runtime tests pass | ✅ Configured parse + invalid fallback paths both covered | ✅ Reused existing DI test harness via overridable config map |
| R2 | `tests/Aura.UnitTests/MorningSummary/SqliteMorningSummaryEmissionStoreTests.cs` | Unit | ✅ 22/22 targeted baseline | ✅ Restart-proof test rewritten to require real reopen semantics | ✅ File-backed restart test passes | ✅ Covers write in first process + read after second reopen | ✅ Disabled pooling for deterministic file cleanup |
| R3 | `tests/Aura.UnitTests/MorningSummary/SqliteMorningSummaryEmissionStoreTests.cs` | Unit | ✅ 22/22 targeted baseline | ✅ Added next-day scheduler due assertion before implementation wiring in test setup | ✅ Next-day due test passes | ✅ Contrasts day D emitted vs day D+1 due at target time | ✅ Minimal helper settings provider added for explicit scheduler/store proof |
| R4 | `tests/Aura.UnitTests/MorningSummary/SqliteMorningSummaryEmissionStoreTests.cs` | Unit | ✅ 22/22 targeted baseline | ✅ Added mark→blocked→reset→due-again scenario first | ✅ Reset forced re-emission test passes | ✅ Validates blocked pre-reset and due post-reset | ✅ Reused scheduler/store integration-style setup in same suite |
| R5 | `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs` | Integration | ✅ 4/4 focused baseline | ✅ Added hosted-service presence assertion first | ✅ Composition assertion passes | ✅ Existing composition cases still pass (now 5/5) | ✅ Kept registration explicit in test host builder |

### Remediation Test Summary

- **New tests added**: 5
  - Unit: 4
  - Integration: 1
- **Existing test updated**: 1
  - `HasBeenEmittedAsync_PersistsAcrossRepositoryRestart_OnSameDatabase` now uses true file-backed reopen semantics
- **Focused runs after remediation**:
  - `Aura.UnitTests` filtered suite: **19/19 passing**
  - `Aura.IntegrationTests` Workers composition suite: **5/5 passing**

### Remediation Files Modified

- `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs`
- `tests/Aura.UnitTests/MorningSummary/SqliteMorningSummaryEmissionStoreTests.cs`
- `tests/Aura.IntegrationTests/Workers/WorkersHostCompositionTests.cs`
- `openspec/changes/w2-h5-t3/apply-progress.md`

## Status

- **18/18 tasks complete**
- **Remediation addendum complete (5/5 verify gaps closed)**
- **Ready for `sdd-verify` rerun**
