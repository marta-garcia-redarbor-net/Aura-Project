# Exploration: w4-demo-ready

## Current State

Demo data already enters the system through `SeedDataHostedService`, which creates realistic Teams, Outlook, Calendar, and PR scenarios and writes them into the normal work-item/calendar stores. From there, connector execution persists items, then `ExecuteConnectorUseCase` runs interruption evaluation after successful persistence and enqueues notification/outbox records for `InterruptNow` results.

The interruption engine is currently deterministic: it resolves focus state, loads triage policy, applies explicit overrides, scores priority, evaluates rules, builds a text explanation, and persists an `InterruptionDecisionRecord` through `IInterruptionDecisionStore`. The decision log is already exposed through `/api/triage/decisions` and rendered in the Blazor `DecisionLog` page.

Qdrant is currently a semantic index/retrieval subsystem plus a readiness signal. It writes and retrieves embeddings, but it is not consulted during interruption decision-making today.

## Affected Areas

- `src/Aura.Infrastructure/Adapters/SeedData/SeedDataHostedService.cs` — curated demo scenarios and metadata shape.
- `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` — persistence-to-decision handoff.
- `src/Aura.Infrastructure/Adapters/Services/InterruptionPolicyEngine.cs` — current triage/interrupt decision core.
- `src/Aura.Infrastructure/Adapters/Decisions/*` — decision trace persistence.
- `src/Aura.Api/Endpoints/TriageEndpoints.cs` and `src/Aura.UI/Pages/DecisionLog.razor` — jury-visible audit surface.
- `src/Aura.Infrastructure/Adapters/SemanticIndex/*` — Qdrant write/read path.
- `src/Aura.Infrastructure/Adapters/Embedding/*` — reusable LLM/embedding provider infrastructure.

## Approaches

1. **Augment the existing interruption engine** — add LLM-assisted reasoning and richer decision evidence to the current policy engine and decision store.
   - Pros: smallest architectural drift; keeps the decision flow in one place; easy to explain to the jury.
   - Cons: engine grows more complex; needs careful boundary control so LLM stays in Infrastructure.
   - Effort: Medium

2. **Build a separate decision-assistant service** — keep the deterministic engine and call an LLM/Qdrant-backed advisor before final verdict.
   - Pros: cleaner separation; easier to toggle on/off for demo.
   - Cons: more moving parts; higher risk of duplicate logic and hidden coupling.
   - Effort: High

## Recommendation

Use the existing interruption engine as the integration point, and enrich the decision record with structured evidence, LLM rationale, and optional Qdrant-retrieved context. Keep Qdrant as support for retrieval/explanation, not as a parallel decision system.

## Risks

- LLM output can become non-deterministic unless its role is constrained to explanation and advisory scoring.
- Decision-trace expansion may require schema and UI changes across both SQLite and EF stores.
- Qdrant adds value only if retrieved context is actually surfaced in the final trace.

## Ready for Proposal

Yes — the current codebase has enough seams to propose the change safely.
