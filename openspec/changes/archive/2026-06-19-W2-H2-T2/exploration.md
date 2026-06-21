## Exploration: W2-H2-T2 — Connector execution flow

### Current State
W2-H2-T2 maps to the backlog item **“Implementar flujo de ejecución por conector”**. The repo already has the W2-H2-T1 checkpoint contract in place (`IIngestionCheckpointStore`, `CheckpointIdentity`, `IngestionCheckpoint`), but there is no dedicated ingestion orchestrator or connector execution loop yet. The ingestion docs still describe the area as a placeholder and defer runtime orchestration, delta sync, and adapter behavior to W2-H2-T2/T3.

### Affected Areas
- `StoryBacklog.md` — defines W2-H2-T2 as the connector execution slice.
- `docs/architecture/ingestion/00-overview.md` — says ingestion must cover checkpoints, delta sync, idempotency, and adapter structure.
- `docs/architecture/ingestion/05-normalization-checkpoints.md` — defers concrete runtime behavior to W2-H2-T2/T3.
- `src/Aura.Application/Ports/IIngestionCheckpointStore.cs` — current contract the orchestrator will consume.
- `src/Aura.Application/Models/CheckpointIdentity.cs` and `src/Aura.Application/Models/IngestionCheckpoint.cs` — current checkpoint shape the flow must honor.
- `src/Aura.Workers/Program.cs` and `src/Aura.Infrastructure/DependencyInjection.cs` — likely wiring points for a future ingestion runner.
- `tests/Aura.UnitTests/`, `tests/Aura.IntegrationTests/`, `tests/Aura.ArchitectureTests/` — will need coverage once the execution flow exists.

### Approaches
1. **Application use case + worker orchestration** — define the connector execution as an Application-level use case, keep scheduling and host wiring in `Workers`, and let Infrastructure only implement connector adapters.
   - Pros: best Clean Architecture fit, easiest to test, keeps business flow out of host code.
   - Cons: needs a few new ports/DTOs before execution can move.
   - Effort: Medium

2. **Worker-first execution loop** — implement the connector iteration directly in `Aura.Workers` and call existing application ports from there.
   - Pros: fastest path to a runnable loop.
   - Cons: risks moving orchestration logic into the host layer and blurring architecture boundaries.
   - Effort: Low/Medium

### Recommendation
Use the Application use case + worker orchestration approach. The repo already treats `Application` as the home for domain-facing contracts, and the checkpoint contract is already there; the missing piece is the runtime orchestration by connector, not another persistence abstraction.

### Risks
- The term “connector” is still broad; the next phase must pin down whether it means Graph, GitHub, or a shared abstraction.
- If orchestration lands in `Workers`, business rules can leak into the host layer.
- The first execution loop can easily overreach into checkpoint persistence and idempotency before T2/T3 are ready.

### Ready for Proposal
Yes — the identifier is specific enough to proceed. The next phase should propose a narrow connector execution use case and keep checkpoint persistence separate.
