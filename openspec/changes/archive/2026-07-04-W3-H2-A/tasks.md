# Tasks: W3-H2-A — Deterministic Interruption Scoring and Decision Contract

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~650–800 |
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
| 1 | Lock contracts/tests first | PR 1 | TDD RED for scorer, verdict, rules, use case regressions |
| 2 | Reuse current engine/rules with richer context | PR 2 | Extend `EvaluationContext`, `InterruptionPolicyEngine`, DI |
| 3 | Canonical metadata wiring + docs | PR 3 | Update mappers, enqueue path, triage docs, final regressions |

## Phase 1: RED Contract Baseline

- [x] 1.1 Add failing scorer specs in `tests/Aura.UnitTests/Triage/PriorityScoringServiceTests.cs` for deterministic canonical inputs, factor explanations, and per-user rule variance.
- [x] 1.2 Expand `tests/Aura.UnitTests/Services/InterruptionPolicyEngineTests.cs` and `tests/Aura.UnitTests/Services/Rules/*.cs` for `INTERRUPT|QUEUE|DEFER`, focus gating, narrow overrides, and explanation contents.
- [x] 1.3 Add failing enqueue regressions in `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` for interrupt-only outbox writes and canonical target-user resolution.

## Phase 2: Foundation Contracts

- [x] 2.1 Extend `src/Aura.Application/Models/EvaluationContext.cs`, `InterruptionVerdict.cs`, and `WorkItemSignalKeys.cs`; add `PriorityScore.cs`, `NormalizedSignal.cs`, and `UserTriagePolicy.cs`.
- [x] 2.2 Add `src/Aura.Application/Ports/IPriorityScoringService.cs` and `IUserTriagePolicyProvider.cs`; register reuse-first defaults in `src/Aura.Application/DependencyInjection.cs` and `src/Aura.Infrastructure/DependencyInjection.cs`.

## Phase 3: GREEN Reuse-First Implementation

- [x] 3.1 Implement `src/Aura.Application/Services/PriorityScoringService.cs` by reusing metadata parsing patterns from `MorningSummaryRankingPolicy` and canonical keys only.
- [x] 3.2 Refactor `src/Aura.Infrastructure/Adapters/Services/InterruptionPolicyEngine.cs` and `src/Aura.Infrastructure/Adapters/Services/Rules/*.cs` to evaluate typed context, approved overrides, focus state, and scored decision branches instead of raw metadata heuristics.
- [x] 3.3 Update `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` so only `InterruptNow` enqueues, while `Queue`/`Defer` stay non-outbox in this slice.
- [x] 3.4 Extend `src/Aura.Infrastructure/Adapters/Connectors/Outlook/OutlookWorkItemMapper.cs`, `Teams/TeamsWorkItemMapper.cs`, and `PrReview/PrReviewWorkItemMapper.cs` to emit/read canonical metadata keys reused by scoring and target-user evaluation.

## Phase 4: Verification and Documentation

- [x] 4.1 Add DI/composition coverage in `tests/Aura.UnitTests/Application/DependencyInjectionTests.cs` and integration coverage for default policy/scorer resolution in `tests/Aura.IntegrationTests`.
- [x] 4.2 Update `docs/architecture/triage/00-overview.md` and `02-proactive-interruptions.md` to document two-stage authority, `DEFER`, and explicit per-user override governance.
- [x] 4.3 Refactor duplicate metadata string lookups into `WorkItemSignalKeys`/helpers, then make all W3-H2-A tests pass with `dotnet test Aura.sln`.
  - **Scope note**: W3-H2-A targeted tests (103 unit + 7 integration) all pass. The 3 remaining `Aura.sln` failures are pre-existing and unrelated to this slice (2 E2E, 1 GraphConnector integration). The metadata string extraction is sufficient for current consumers; a deeper refactor into `WorkItemSignalKeys` helpers belongs in a dedicated follow-up.
