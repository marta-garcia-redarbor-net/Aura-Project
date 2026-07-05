# Proposal: W3-H2-A — Deterministic Interruption Scoring and Decision Contract

## Intent

W3-H2 was too broad to review safely. This slice implements backlog T1 plus the decision-behavior core of T2: deterministic priority/attention scoring and an explainable interruption contract. It closes the current gaps called out by exploration: thin `IInterruptionPolicyEngine`, no `DEFER`, `EvaluationContext` limited to `WorkItem`, stub focus-state consumption, and no scoring service implementation.

## Scope

### In Scope
- Define deterministic priority and attention scoring with rules made explicit in tests.
- Expand the interruption decision contract to support explainable `INTERRUPT|QUEUE|DEFER` outcomes.
- Update policy evaluation to consume richer context, including focus-state input, while keeping final authority in `IInterruptionPolicyEngine`.

### Out of Scope
- Durable audit trail, outbox/schema changes, worker propagation, and dispatch payload expansion (`W3-H2-T3` / later slice).
- `W3-H2-T4` Teams preliminary scoring in connector logic; connector scoring remains future and non-authoritative.

## Capabilities

### New Capabilities
- `priority-scoring`: deterministic global priority/attention scoring used as policy input.
- `interruption-policy-engine`: explainable final decision contract and rules for `INTERRUPT|QUEUE|DEFER`.

### Modified Capabilities
- `triage-global-policy`: clarify that connector scores stay input-only and final triage authority includes explicit defer behavior.

## Approach

Implement scoring and decision rules in Application-layer contracts and testable policy logic, then update the engine implementation to evaluate a richer `EvaluationContext`. Consume `IFocusStateResolver` as an input to policy evaluation, but do not add audit persistence in this slice. Keep connector-owned logic limited to preliminary signals.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Application/Ports/IInterruptionPolicyEngine.cs` | Modified | Replace thin contract with explicit decision model. |
| `src/Aura.Application/Models/InterruptionVerdict.cs` | Modified | Add `DEFER` and structured explainable outcome shape. |
| `src/Aura.Application/Models/EvaluationContext.cs` | Modified | Carry policy inputs beyond `WorkItem`. |
| `src/Aura.Infrastructure/Adapters/Services/InterruptionPolicyEngine.cs` | Modified | Apply deterministic scoring and interruption/defer rules. |
| `tests/Aura.UnitTests/Services/InterruptionPolicyEngineTests.cs` | Modified | Make formulas and decisions explicit in tests. |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Decision contract remains ambiguous for the audit slice | Med | Lock outcome shape and explainability in specs/tests now. |
| Connector authority leaks into this slice | Med | Keep T4 out of scope and restate engine-only authority in specs. |

## Rollback Plan

Revert the scoring contract, `DEFER` verdict support, and policy-engine behavior to the current `InterruptNow|Queue` flow. This slice avoids audit-storage changes, so rollback does not require schema reversal.

## Dependencies

- `openspec/changes/W3-H2/exploration.md`
- `openspec/specs/focus-state-machine/spec.md`

## Success Criteria

- [ ] T1 scoring rules are explicit and deterministic in automated tests.
- [ ] The engine returns explainable `INTERRUPT|QUEUE|DEFER` decisions from a richer evaluation context.
- [ ] Connector-owned scoring remains preliminary only, and no audit persistence is introduced in this slice.
