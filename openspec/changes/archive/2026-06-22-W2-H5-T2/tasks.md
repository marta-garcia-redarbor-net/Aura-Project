# Tasks: W2-H5-T2 Morning Summary Ranking Policy Implementation

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 620-940 |
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
| 1 | Deliver ranking policy core + spec-scenario RED/GREEN tests | PR 1 | `RankingFactor`, `WorkItemSignalKeys`, ranking port + policy + `MorningSummaryRankingPolicyTests` |
| 2 | Wire ranking into composer path + DI registration | PR 2 | `MorningSummaryComposer`, `DependencyInjection`, DI/composer tests; depends on Unit 1 |
| 3 | Lock architecture boundary + full regression evidence | PR 3 | `MorningSummaryArchitectureTests` + solution test pass evidence; depends on Unit 2 |

## Phase 1: Foundation (TDD RED)

- [x] 1.1 Create `tests/Aura.UnitTests/Triage/MorningSummaryRankingPolicyTests.cs` with failing tests for explicit order scenarios: Deadline resolves first, then Impact, then Risk.
- [x] 1.2 In the same test file, add failing scenarios for single `PreliminaryScore` usage (post-explicit tie + all-explicit-absent fallback) and forbid split-rule behavior.
- [x] 1.3 Add failing tests for deterministic tie chain (nearest due date → oldest `CreatedAt` → lexical `ExternalId`) and `insufficient-signals` last with empty `RankingExplanation.Contributions`.
- [x] 1.4 Create `tests/Aura.UnitTests/Triage/MorningSummaryComposerTests.cs` with failing tests proving composer returns ordered entries and per-item explanations aligned to ranking output.

## Phase 2: Core Policy Implementation (TDD GREEN)

- [x] 2.1 Modify `src/Aura.Application/Models/RankingFactor.cs` to add `PreliminaryScore` while preserving existing factors.
- [x] 2.2 Create `src/Aura.Application/Models/WorkItemSignalKeys.cs` with constants for `outlook.deadline.cue`, `outlook.deadline.source`, `outlook.scoring.totalScore`, and `teams.priority.raw`.
- [x] 2.3 Create `src/Aura.Application/Ports/IMorningSummaryRankingPolicy.cs` with deterministic `Rank(IReadOnlyList<WorkItem>)` contract returning `IReadOnlyList<RankedWorkItem>`.
- [x] 2.4 Create `src/Aura.Application/UseCases/MorningSummary/MorningSummaryRankingPolicy.cs` implementing spec decision order, single preliminary-score input, tie chain, and structured explanations.

## Phase 3: Composer Wiring & DI (TDD GREEN)

- [x] 3.1 Create `src/Aura.Application/UseCases/MorningSummary/MorningSummaryComposer.cs` that reads via `IWorkItemReader`, calls `IMorningSummaryRankingPolicy`, and returns `MorningSummary` entries.
- [x] 3.2 Update `src/Aura.Application/DependencyInjection.cs` to register `IMorningSummaryRankingPolicy` and `IMorningSummaryComposer` without introducing Infrastructure dependencies.
- [x] 3.3 Update `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs` and `tests/Aura.UnitTests/Triage/MorningSummaryComposerTests.cs` to make DI/composer-path tests pass.

## Phase 4: Architecture Guard & Verification (TDD REFACTOR)

- [x] 4.1 Modify `tests/Aura.ArchitectureTests/MorningSummaryArchitectureTests.cs` to assert `MorningSummaryRankingPolicy` resides in `Aura.Application` and not connectors/Infrastructure.
- [x] 4.2 Add architecture/test assertion that ranking path references no AI prioritization port or implementation.
- [x] 4.3 Run `dotnet test Aura.sln` and record evidence for ranking scenarios, composer wiring, and architecture boundaries in PR verification notes.
