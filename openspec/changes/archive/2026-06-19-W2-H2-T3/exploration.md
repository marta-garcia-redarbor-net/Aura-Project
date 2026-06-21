## Exploration: W2-H2-T3 — Checkpoint persistence

### Current State
The codebase already has the checkpoint contract in `Aura.Application` (`IIngestionCheckpointStore`, `CheckpointIdentity`, `IngestionCheckpoint`) and a working connector execution flow that reads checkpoints to bound the fetch window. `ExecuteConnectorUseCase` is explicitly read-only today: it calls `GetAsync`, derives `WindowStart`, and defers all checkpoint writes. `InMemoryIngestionCheckpointStore` already supports both `GetAsync` and `SaveAsync`, and the backlog item `W2-H2-T3` is specifically “Guardar y recuperar checkpoints”.

### Affected Areas
- `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` — current read-only boundary will need checkpoint write behavior.
- `src/Aura.Application/Ports/IIngestionCheckpointStore.cs` — existing port already exposes read/write; persistence semantics must be honored.
- `src/Aura.Infrastructure/Adapters/Ingestion/InMemoryIngestionCheckpointStore.cs` — current adapter is the persistence mechanism for the slice.
- `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` — needs persistence and idempotency coverage.
- `tests/Aura.UnitTests/Workers/ConnectorExecutionWorkerTests.cs` — may need assertions if execution now persists checkpoints.
- `tests/Aura.ArchitectureTests/IngestionArchitectureTests.cs` — architecture boundaries must still prevent Infrastructure leakage.
- `StoryBacklog.md` — W2-H2-T3 is the completion target for this slice.

### Approaches
1. **Persist in the Application use case** — after a successful execution, the use case writes the updated checkpoint through `IIngestionCheckpointStore`.
   - Pros: keeps orchestration and persistence together in one domain-facing flow; easiest to test; minimal extra plumbing.
   - Cons: use case grows to include state mutation and persistence policy.
   - Effort: Low/Medium

2. **Split checkpoint mutation into a dedicated Application service** — the use case returns execution data, and a separate service computes/persists the new checkpoint.
   - Pros: cleaner separation between execution and checkpoint policy; easier to evolve if checkpoint rules get richer.
   - Cons: extra abstraction for a small slice; more wiring and more tests.
   - Effort: Medium

### Recommendation
Use the Application use case to persist checkpoints for this slice. The repo already has the port, the window calculation, and a simple in-memory adapter; the missing behavior is a straightforward read-after-execute write. A separate service would be over-architecture at this stage.

### Risks
- Writing checkpoints too early could record failed or partial runs and break idempotency.
- If checkpoint updates happen outside the Application boundary, the worker layer will absorb business policy and violate Clean Architecture.

### Ready for Proposal
Yes — the slice is concrete enough. The proposal should define when the checkpoint is written, what payload is stored, and how a second execution proves idempotent behavior.
