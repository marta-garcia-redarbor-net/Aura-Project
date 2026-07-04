# Design: W3-H2-A

## Technical Approach

Implement deterministic triage in two explicit steps: (1) Application builds a normalized evaluation context from canonical `WorkItem.Metadata`, focus state, and approved per-user policy inputs; (2) the interruption engine evaluates that context and returns an explainable `INTERRUPT | QUEUE | DEFER` verdict. Connector metadata remains input-only, Qdrant stays out of the decision path, and no outbox/audit persistence changes are introduced.

## Architecture Decisions

| Decision | Options / tradeoff | Chosen approach / rationale |
|---|---|---|
| Scoring location | Keep scoring inside connector rules vs centralize after ingestion | Centralize in Application (`IPriorityScoringService`) so the same metadata produces one deterministic score per user policy, independent of connector implementation. |
| Mixed signal types | Force all signals to bool or numeric vs typed normalized signals | Use typed normalized signals (`BooleanSignal`, `LevelSignal`, optional numeric/text payloads) inside `EvaluationContext`; this cleanly supports `vip_sender`, `ack_only`, `time_criticality`, and `message_length_bucket` without stringly-typed branching. |
| Final decision orchestration | Push decision logic into connectors/use case vs keep engine authority | Keep `IInterruptionPolicyEngine` as sole decision authority. `ExecuteConnectorUseCase` still passes only `WorkItem`; the engine enriches context with focus state and user policy to avoid spreading triage logic. |
| Per-user adjustment hook | Implement persistence now vs define contract only | Define `IUserTriagePolicyProvider` now and register a default provider returning baseline policy plus approved explicit overrides. Review-first suggestions are modeled in the contract but not auto-applied in this slice. |

## Data Flow

`Connector -> WorkItem(metadata) -> ExecuteConnectorUseCase -> IInterruptionPolicyEngine`
`                                       |`
`                                       +-> IFocusStateResolver`
`                                       +-> IUserTriagePolicyProvider`
`                                       +-> IPriorityScoringService`
`                                                -> Interruption rules -> InterruptionVerdict`

The engine builds `EvaluationContext(userId, focusState, normalizedSignals, priorityScore, approvedPolicy, workItem)`. Rules evaluate typed context in order: explicit approved override, focus-state gate, scored decision branch, fallback queue. Only `INTERRUPT` continues to the existing outbox path; `QUEUE` and `DEFER` remain non-enqueued in this slice.

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Application/Models/EvaluationContext.cs` | Modify | Carry `UserId`, `FocusState`, normalized signals, `PriorityScore`, and approved user policy alongside `WorkItem`. |
| `src/Aura.Application/Models/InterruptionVerdict.cs` | Modify | Add `Defer`, explanation fields, decisive rule key, and scored-context summary. |
| `src/Aura.Application/Models/WorkItemSignalKeys.cs` | Modify | Centralize canonical metadata keys consumed by normalization/scoring. |
| `src/Aura.Application/Models/PriorityScore.cs` | Create | Typed score result with factor contributions and human-readable explanation. |
| `src/Aura.Application/Models/NormalizedSignal.cs` | Create | Mixed signal contract for boolean/level-based normalized inputs. |
| `src/Aura.Application/Models/UserTriagePolicy.cs` | Create | Approved per-user rule set plus explicit-override hook and review-first suggestion placeholders. |
| `src/Aura.Application/Ports/IPriorityScoringService.cs` | Create | Application contract for deterministic scoring from normalized context. |
| `src/Aura.Application/Ports/IUserTriagePolicyProvider.cs` | Create | Reads approved per-user policy/override inputs without defining persistence details. |
| `src/Aura.Application/Services/PriorityScoringService.cs` | Create | Deterministic scorer using canonical metadata + normalized signals only. |
| `src/Aura.Infrastructure/Adapters/Services/InterruptionPolicyEngine.cs` | Modify | Enrich context, call scorer/provider/resolver, and evaluate explainable `INTERRUPT|QUEUE|DEFER`. |
| `src/Aura.Infrastructure/Adapters/Services/Rules/*` | Modify | Replace current threshold/VIP/keyword raw-metadata checks with context-based override, focus-gate, and decision rules. |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modify | Register scorer, policy provider, and updated rule set. |
| `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` | Modify | Preserve current call shape, stop assuming only interrupt/queue, and keep outbox write limited to `INTERRUPT`. |
| `docs/architecture/triage/00-overview.md` | Modify | Document two-stage boundary and global authority for `INTERRUPT|QUEUE|DEFER`. |
| `docs/architecture/triage/02-proactive-interruptions.md` | Modify | Document explainability, per-user adjustment contract, and review-first generalizations. |
| `tests/Aura.UnitTests/Services/InterruptionPolicyEngineTests.cs` | Modify | Lock verdict selection, explanation contents, and `DEFER` behavior. |
| `tests/Aura.UnitTests/Triage/PriorityScoringServiceTests.cs` | Create | Prove deterministic scoring and factor explanations. |
| `tests/Aura.UnitTests/Services/Rules/*.cs` | Modify | Verify override, focus-state, and decision-rule branches against typed context. |

## Interfaces / Contracts

```csharp
public interface IPriorityScoringService
{
    PriorityScore Score(EvaluationContext context);
}

public interface IUserTriagePolicyProvider
{
    Task<UserTriagePolicy> GetApprovedPolicyAsync(string userId, CancellationToken ct);
}
```

`PriorityScore` exposes total + factor contributions; `InterruptionVerdict` exposes final decision + explanation; `UserTriagePolicy` carries only explicit approved policy inputs in this slice.

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | Signal normalization, score determinism, verdict branching, explanations | New scorer tests plus updated engine/rule tests with fixed metadata and focus states. |
| Integration | DI composition and default policy-provider behavior | Extend store/composition tests to verify the engine resolves scorer/provider/rules without Infrastructure leakage into Application ports. |
| E2E | None in this slice | Existing notification/browser flows are unchanged because only `INTERRUPT` still reaches the current outbox path. |

## Migration / Rollout

No migration required. No outbox schema expansion, no audit storage, and no worker propagation redesign are part of this slice.

## Open Questions

- [ ] `IFocusStateResolver` still uses `string userId`, while `openspec/specs/focus-state-machine/spec.md` describes a `UserId` type. This slice can proceed with `string` for compatibility, but the spec/code mismatch should be resolved explicitly.
- [ ] Workers currently fall back to `item.Source` when `assignedTo` is absent. We need one approved canonical metadata key for the target user before implementation relies on per-user policy/focus evaluation.
