# Tasks: W2-H5-T3 Morning Summary Timezone Scheduling

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 520-760 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | ask-always |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Contracts + RED tests | PR 1 | If feature-branch-chain: base = feature/tracker branch |
| 2 | Provider + SQLite guard + GREEN tests | PR 2 | If feature-branch-chain: base = PR 1 branch |
| 3 | Worker/config/DI wiring + verification | PR 3 | If feature-branch-chain: base = PR 2 branch |

## Phase 1: Foundation / Contracts (TDD-RED)

- [x] 1.1 Create `src/Aura.Application/Ports/IMorningSummarySettingsProvider.cs`, `IMorningSummaryEmissionStore.cs`, and `IMorningSummaryScheduler.cs`.
- [x] 1.2 Create `src/Aura.Application/Models/MorningSummarySettings.cs` and `MorningSummaryDueState.cs`.
- [x] 1.3 RED: add `tests/Aura.UnitTests/MorningSummary/MorningSummarySchedulerTests.cs` for settings chain, DST wall-clock, and due/not-due.
- [x] 1.4 RED: add `tests/Aura.UnitTests/MorningSummary/SqliteMorningSummaryEmissionStoreTests.cs` for duplicate block, next-day reset, restart survival, and override reset.

## Phase 2: Core Implementation (TDD-GREEN)

- [x] 2.1 Implement `src/Aura.Application/UseCases/MorningSummaryScheduling/MorningSummaryScheduler.cs` with configured → system → UTC resolution and guard lookup.
- [x] 2.2 Create `src/Aura.Infrastructure/Adapters/MorningSummaryScheduling/MorningSummaryOptions.cs` and `AppSettingsMorningSummarySettingsProvider.cs` (`TargetLocalTime` default `09:00`).
- [x] 2.3 Implement `src/Aura.Infrastructure/Adapters/MorningSummaryScheduling/SqliteMorningSummaryEmissionStore.cs` on shared `aura.db` with PK `(UserId, LocalDate)` and `Has/Mark/Reset`.
- [x] 2.4 Create `src/Aura.Infrastructure/Adapters/MorningSummaryScheduling/DependencyInjection.cs` to register options + provider + emission store.

## Phase 3: Integration / Wiring

- [x] 3.1 Update `src/Aura.Application/DependencyInjection.cs` to register `IMorningSummaryScheduler` with `MorningSummaryScheduler`.
- [x] 3.2 Update `src/Aura.Infrastructure/DependencyInjection.cs` to call `AddMorningSummarySchedulingAdapters(configuration)`.
- [x] 3.3 Create `src/Aura.Workers/MorningSummarySchedulingWorker.cs` using fixed system-level user identity and `[LoggerMessage]` traces.
- [x] 3.4 Update `src/Aura.Workers/Program.cs` and `src/Aura.Workers/appsettings.json` to wire the hosted service and MorningSummary/ConnectionStrings for `aura.db`.

## Phase 4: Testing / Verification (TDD-GREEN→REFACTOR)

- [x] 4.1 GREEN: make scheduler tests pass for settings resolution, DST correctness, due-state fields, and same-day guard block.
- [x] 4.2 GREEN: make SQLite store tests pass for mark, duplicate, reset, and restart behavior.
- [x] 4.3 Add/update DI and worker wiring tests in `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` and `tests/Aura.UnitTests/Workers/MorningSummarySchedulingWorkerTests.cs`.
- [x] 4.4 Run `dotnet test Aura.sln` and fix regressions within W2-H5-T3 scope only.

## Phase 5: Cleanup / Scope Guard

- [x] 5.1 REFACTOR: remove timezone/date conversion duplication; keep scheduler pure (no emission mutation).
- [x] 5.2 Add scope guard notes/assertions so override seam stays programmatic-only and UI stays out of scope.
