# Proposal: Demo-Ready Decisioning, Traceability, and Curated Scenarios

## Intent

The TFM demo must convince a jury that Aura makes credible, explainable interruption decisions — not random noise. Today the interruption engine is purely deterministic, Qdrant is disconnected from decisioning, and seed data is generic. The jury cannot follow *why* an item interrupted. This change adds bounded LLM-assisted decisioning, an exhaustive per-item decision trace, visible Qdrant-retrieved context, and curated realistic scenarios.

## Scope

### In Scope
- Bounded LLM advisory inside `InterruptionPolicyEngine`: may modify the deterministic verdict only under explicit guardrails, with mandatory structured reasoning.
- Decision-time Qdrant retrieval surfaced as visible evidence before the verdict.
- Exhaustive per-work-item decision trace (signals, rules fired, retrieved context, LLM rationale, guardrail outcome) via the existing record/API/UI seams.
- Curated Teams + Outlook + PR demo scenarios replacing weak/random seed data.

### Out of Scope
- Opaque online learning or model fine-tuning.
- A parallel decision or trace subsystem.
- New vector store or connector sources.

## Capabilities

### New Capabilities
- `llm-decision-advisor`: bounded advisory layer that reviews the deterministic verdict, may adjust it only under explicit guardrails, and emits auditable structured reasoning.

### Modified Capabilities
- `interruption-policy-engine`: decision record extended with structured evidence, retrieved semantic context, LLM rationale, and guardrail outcome; deterministic baseline preserved as authority.
- `interruption-decision-log`: full decision trace per work item exposed in the jury-facing audit view.
- `semantic-index`: add a decision-time retrieval port consumed by interruption decisioning.
- `demo-mode`: curated realistic Teams/Outlook/PR scenarios.

## Approach

Reuse the existing engine as the single insertion point. Order: (1) semantic retrieval port + decision-record schema, (2) LLM advisor with guardrails, (3) trace surfacing in API/UI, (4) curated scenarios last. The deterministic rules stay the source of truth; the LLM is a bounded modifier that must justify any change.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Adapters/Services/InterruptionPolicyEngine.cs` | Modified | Advisor + retrieval hooks |
| `src/Aura.Infrastructure/Adapters/Decisions/*` | Modified | Trace persistence schema |
| `src/Aura.Infrastructure/Adapters/SemanticIndex/*` | Modified | Decision-time retrieval |
| `src/Aura.Api/Endpoints/TriageEndpoints.cs`, `src/Aura.UI/Pages/DecisionLog.razor` | Modified | Full trace surface |
| `src/Aura.Infrastructure/Adapters/SeedData/SeedDataHostedService.cs` | Modified | Curated scenarios |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| LLM non-determinism | High | Deterministic baseline authoritative; LLM change gated by guardrails + logged rationale |
| Trace schema drift (SQLite/EF) | Med | Extend existing record; additive migration |
| Qdrant adds no visible value | Med | Retrieved context rendered in trace |

## Rollback Plan

Feature flag disables the LLM advisor and decision-time retrieval, reverting to the deterministic engine. Trace additions are additive columns; curated seed data is data-only and reversible.

## Dependencies

- Embedding/LLM provider infrastructure (existing).
- Populated Qdrant collections for retrieval.

## Success Criteria

- [ ] Jury can open any work item and see the full decision trace.
- [ ] Any LLM-modified verdict shows explicit reasoning and guardrail outcome.
- [ ] Qdrant-retrieved context is visible in the trace.
- [ ] Teams, Outlook, and PR scenarios each demonstrate a distinct decision path.
