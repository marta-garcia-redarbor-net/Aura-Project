## Exploration: W2-H2-T1 — Checkpoint store contract

### Current State
W2-H2-T1 is the first slice of the ingestion orchestrator story. The backlog and plan point to a checkpoint store interface that will support repeated ingestion runs without duplicates, but the repo does not yet define a dedicated checkpoint abstraction. Today, the only persistence seam in this area is `ISemanticOutboxRepository`, which belongs to semantic index sync, not ingestion checkpoints.

### Affected Areas
- `StoryBacklog.md` — names the task and its DoD/risk.
- `StoryPlan.md` — places the work inside the ingestion orchestrator slice with checkpoint/idempotency goals.
- `docs/architecture/ingestion/00-overview.md` — says ingestion must cover checkpoints, delta sync, and idempotency.
- `docs/architecture/ingestion/05-normalization-checkpoints.md` — placeholder for the contract this task likely needs.
- `src/Aura.Application/Ports/` — best place for the checkpoint-store port.
- `src/Aura.Infrastructure/Adapters/` — likely place for the first persistence implementation.
- `tests/Aura.UnitTests/` and `tests/Aura.IntegrationTests/` — will need contract and persistence tests once implementation starts.

### Approaches
1. **Application port + SQLite adapter** — define a small checkpoint-store interface in Application and back it with a simple SQLite adapter in Infrastructure.
   - Pros: fits Clean Architecture, keeps the contract explicit, easy to test.
   - Cons: introduces persistence shape before orchestration logic is finished.
   - Effort: Medium

2. **In-memory checkpoint seam first** — define the contract and keep the initial implementation in-memory behind the port, deferring persistence.
   - Pros: fastest way to unblock orchestration tests.
   - Cons: risks false confidence if persistence semantics differ later.
   - Effort: Low

### Recommendation
Use an Application port now, but keep the first contract minimal: store/retrieve checkpoint by connector/source key with a watermark or cursor value and processed timestamp. That is enough to unblock W2-H2-T2/T3 without over-designing the storage model.

### Risks
- Over-specified checkpoint fields could lock the ingestion design too early.
- If the contract does not include a stable connector/source key, later deduplication will drift.
- A fake in-memory seam would not prove repeat-run persistence behavior.

### Ready for Proposal
Yes — the evidence is enough to propose a narrow checkpoint-store port slice, but not enough to define the full ingestion pipeline yet.
