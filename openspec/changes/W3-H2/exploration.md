## Exploration: W3-H2 — Interruption Engine

### Current State
Aura already has a basic interruption pipeline. `IInterruptionPolicyEngine` evaluates a `WorkItem`, `InterruptionPolicyEngine` runs registered rules, and `ExecuteConnectorUseCase` enqueues notifications only when the verdict is `InterruptNow`. The current model is still incomplete for W3-H2: `InterruptionDecision` only supports `InterruptNow` and `Queue`, `EvaluationContext` contains only the `WorkItem`, `FocusStateResolver` is still a placeholder that always returns `WindowOfOpportunity`, and the outbox/worker path only preserves `TriggerRule`, not a full audit-ready explanation. There is also no `IPriorityScoringService` implementation in code yet, even though the architecture map reserves that capability. T4 must remain out of scope here because the documented boundary says connectors provide preliminary signals only and final authority stays in `IInterruptionPolicyEngine`.

### Affected Areas
- `src/Aura.Application/Ports/IInterruptionPolicyEngine.cs` — current contract only evaluates a `WorkItem`; W3-H2 needs a richer decision contract.
- `src/Aura.Application/Models/InterruptionVerdict.cs` — missing `DEFER` and missing a structured decision explanation beyond `TriggerRule`.
- `src/Aura.Application/Models/EvaluationContext.cs` — currently carries only `WorkItem`, so focus state and other policy inputs have nowhere to live.
- `src/Aura.Application/Ports/IFocusStateResolver.cs` — W3-H2 is the first real consumer according to archived W3-H1 design.
- `src/Aura.Application/Services/FocusStateResolver.cs` — still a stub; W3-H2 is where real signal wiring starts.
- `src/Aura.Infrastructure/Adapters/Services/InterruptionPolicyEngine.cs` — current engine is first-match `InterruptNow` vs `Queue`; no deferred path or explanation aggregation policy.
- `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` — current integration only acts on `InterruptNow` and drops the rest after evaluation.
- `src/Aura.Domain/WorkItems/NotificationOutboxEntry.cs` — current outbox entity cannot persist an audit-ready decision reason snapshot.
- `src/Aura.Infrastructure/Adapters/Notifications/SqliteNotificationOutboxStore.cs` — current schema only stores trigger rule, not full traceable reasoning.
- `src/Aura.Api/Workers/WorkItemNotificationWorker.cs` — rebuilds a synthetic verdict from outbox data, which loses the original explanation.
- `src/Aura.Api/Adapters/SignalRWorkItemNotificationDispatcher.cs` — dispatch payload currently exposes only a thin reason string.
- `tests/Aura.UnitTests/Services/InterruptionPolicyEngineTests.cs` and rule tests — existing tests cover simple rule matching, not explicit priority/attention formulas or explainable deferred decisions.

### Approaches
1. **Keep W3-H2 as one change** — Define scoring, add focus-aware decisioning, add `DEFER`, and add audit persistence in a single proposal.
   - Pros: keeps one end-to-end story; no temporary intermediate design between core policy and audit trail.
   - Cons: spans contracts, engine behavior, persistence schema, worker dispatch, and tests at once; likely exceeds the 800-line review budget; harder to review cleanly; mixes T1/T2/T3 concerns in one step.
   - Effort: High

2. **Split W3-H2 into two proposal slices** — First define deterministic scoring + interruption/defer policy, then add audit persistence and pipeline propagation.
   - Pros: better reviewer load control; clearer Clean Architecture boundaries; matches backlog sequencing (`T1` formula first, then `T2` decision behavior, then `T3` durable audit trail); lets proposal 1 settle the core contract before schema and dispatch changes.
   - Cons: requires explicit handoff between slices; proposal 1 must define how proposal 2 will persist the decision explanation.
   - Effort: Medium

### Recommendation
Split it. Keep `W3-H2-T4` out entirely, and propose two smaller changes: **Slice A: scoring + policy contract** (`T1` plus the core of `T2`) and **Slice B: audit trail + pipeline propagation** (`T3` plus the integration part of `T2`). The evidence is strong: the current code has no `DEFER`, no scoring service implementation, no focus-aware evaluation context, and no durable explanation model. That is too much contract and plumbing change for one reviewable proposal, while the backlog already separates formula, decisioning, and audit as distinct work items.

### Risks
- The current doc/code gap around `DEFER` can leak into the proposal if the new decision model is not settled first.
- If audit persistence is designed before the decision explanation shape is finalized, proposal 2 will either duplicate work or lock in the wrong payload.
- W3-H2 can accidentally pull final triage authority into connector logic if T4 is mixed into the same proposal.

### Ready for Proposal
Yes — but not as one large change. The orchestrator should tell the user that the evidence supports splitting W3-H2 into two smaller proposals, with T4 explicitly deferred and kept non-authoritative.
