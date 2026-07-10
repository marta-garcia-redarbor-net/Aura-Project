# Tasks: w4-demo-ready

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 900-1100 |
| 400-line budget risk | High |
| 800-line budget status | Likely exceeds |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 -> PR 2 -> PR 3 -> PR 4 |
| Delivery strategy | single-pr-default |
| Chain strategy | size-exception |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: size-exception
400-line budget risk: High
800-line budget status: Likely exceeds; size:exception required before apply.

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | LLM decisioning foundation | PR 1 | Advisor flow + guardrails + tests |
| 2 | Exhaustive trace persistence/API | PR 2 | EF migration + store + contract tests |
| 3 | Qdrant trace UI | PR 3 | Context retrieval + ordered trace panel |
| 4 | Curated demo scenarios | PR 4 | Teams/Outlook/PR verdict coverage |

## Phase 1: LLM-Assisted Decisioning Infrastructure / Flow

- [x] 1.1 RED: Add guardrail-path tests in `tests/Aura.UnitTests/Adapters/Services/InterruptionPolicyEngineTraceTests.cs` (confirmed/adjusted/blocked/llm-unavailable).
- [x] 1.2 GREEN: Create `src/Aura.Application/Ports/ILlmDecisionAdvisor.cs` and `src/Aura.Application/Models/{AdvisoryRequest.cs,AdvisoryResponse.cs}`.
- [x] 1.3 GREEN: Create `src/Aura.Infrastructure/Adapters/LlmAdvisor/{MeaiLlmDecisionAdvisorAdapter.cs,NullLlmDecisionAdvisor.cs,LlmAdvisorOptions.cs,DependencyInjection.cs}` (JSON contract + timeout).
- [x] 1.4 GREEN: Update `src/Aura.Infrastructure/Adapters/Services/InterruptionPolicyEngine.cs` to run advisor and apply guardrails.
- [x] 1.5 REFACTOR: Add telemetry in `InterruptionPolicyEngine.cs` for guardrail outcome, advisor latency, and fallback reason.

## Phase 2: Traceability (Persistence + API)

- [x] 2.1 RED: Add round-trip tests in `tests/Aura.IntegrationTests/Adapters/Decisions/EfDecisionStoreTraceTests.cs` for trace fields.
- [x] 2.2 GREEN: Extend `src/Aura.Application/Models/InterruptionDecisionRecord.cs` with `RetrievedSemanticContext`, `LlmRationale`, `GuardrailOutcome`.
- [x] 2.3 GREEN: Update `src/Aura.Infrastructure/Adapters/Persistence/AuraDbContext.cs` and create EF migration adding 3 nullable `TEXT` columns to `InterruptionDecisions`.
- [x] 2.4 GREEN: Update `src/Aura.Infrastructure/Adapters/Decisions/EfInterruptionDecisionStore.cs` to serialize/deserialize full trace fields.
- [x] 2.5 VERIFY: Update `src/Aura.Api/Endpoints/TriageEndpoints.cs` and `src/Aura.UI/Models/DecisionLogResponse.cs`; assert contract includes fields.

## Phase 3: Visible Qdrant Participation in Decision Flow

- [x] 3.1 RED: Add `tests/Aura.UnitTests/Adapters/SemanticIndex/QdrantDecisionContextAdapterTests.cs` for success/timeout/exception behaviors.
- [x] 3.2 GREEN: Create `src/Aura.Application/Ports/IDecisionContextRetriever.cs` and `src/Aura.Application/Models/DecisionContextItem.cs`.
- [x] 3.3 GREEN: Create `src/Aura.Infrastructure/Adapters/SemanticIndex/{QdrantDecisionContextAdapter.cs,NullDecisionContextRetriever.cs}` and register DI.
- [x] 3.4 GREEN: Update `src/Aura.Infrastructure/Adapters/Services/InterruptionPolicyEngine.cs` to include context in advisor/store calls.
- [x] 3.5 GREEN: Update `src/Aura.UI/Pages/DecisionLog.razor` to render summary-first rows with expandable trace (`summary -> rules -> rationale -> semantic context`).
- [x] 3.6 VERIFY: Add `tests/Aura.E2E/Triage/DecisionLogTracePanelTests.cs` for expansion, keyboard navigation, and section order.

## Phase 4: Curated Teams/Outlook/PR Scenarios on Top

- [x] 4.1 RED: Add `tests/Aura.IntegrationTests/SeedData/CuratedScenarioCoverageTests.cs` verifying INTERRUPT/QUEUE/DEFER per source.
- [x] 4.2 GREEN: Update `src/Aura.Infrastructure/Adapters/SeedData/SeedDataHostedService.cs` with curated Teams, Outlook, and PR signals.
- [x] 4.3 GREEN: Ensure `SeedDataHostedService.cs` calls `IInterruptionPolicyEngine.EvaluateAsync` after seeding to prebuild traces.
- [x] 4.4 VERIFY: Assert seeded traces include meaningful rationale and semantic context when services are enabled.

## Phase 5: Verification and Cleanup

- [x] 5.1 Add/extend `tests/Aura.ArchitectureTests` rules for Application-owned ports and no Qdrant SDK types in Domain/Application.
- [ ] 5.2 Run `dotnet build Aura.sln` and `dotnet test Aura.sln --collect:"XPlat Code Coverage"`; fix failures in the same slice.
- [x] 5.3 Update `docs/architecture/triage/00-overview.md` with guardrail outcomes, trace panel behavior, and rollback notes.
