# Proposal: W2-H2-T3 — Ingestion Checkpoint Persistence

## Intent

`ExecuteConnectorUseCase` is read-only today: it reads a checkpoint to bound the fetch window but never writes one, so every run re-pulls the same window and incremental sync is impossible. This slice makes the use case persist checkpoints after execution, with explicit, testable policy for which fields advance on full-success, empty, full-failure, and mixed/partial-failure runs. The critical invariant: execution-finished-at must only advance when the run fully succeeded, so a partial failure still re-attempts the unprocessed window next time while never re-pulling already-processed items.

## Scope

### In Scope
- Persist the checkpoint via `IIngestionCheckpointStore.SaveAsync` from the Application use case after execution.
- Store TWO timestamps: max-processed-at (highest timestamp among successfully processed items) and execution-finished-at.
- Field update policy (mutually exclusive outcomes): full-success+new items → both; full-success+no items → finished-at only; full-failure → neither; mixed/partial-failure → max-processed-at ONLY (finished-at stays put).
- Max-processed-at is always derived from the highest timestamp among successfully processed items only; failed items are excluded and remain for later runs.
- Surface a max-processed-item timestamp on the canonical execution result so the use case can apply policy without provider SDK types.
- Idempotency coverage: a second run over the same window does not regress the checkpoint.

### Out of Scope
- Field-level canonical mapping of Teams items (W2-H3).
- Outlook/Calendar/GitHub connectors, scheduling, retry/resilience.
- Durable persistence backend (slice stays on the in-memory adapter).

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `ingestion-checkpoint-store`: checkpoint value shape adds a distinct execution-finished-at timestamp alongside max-processed-at; the two advance independently per run outcome, and a mixed/partial-failure run advances max-processed-at without advancing finished-at.
- `connector-execution`: "Checkpoint Read-Only Integration" becomes read-then-persist; canonical result exposes max-processed-item timestamp.

## Approach

Per exploration recommendation, keep persistence in the Application use case (no extra service). The adapter (Infrastructure) computes the max processed-item timestamp from provider data and returns it in `ConnectorExecutionResult`; the use case applies the field-update policy and calls `SaveAsync`. Business policy stays in Application — Workers stay orchestration-only.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Application/Models/IngestionCheckpoint.cs` | Modified | Add execution-finished-at timestamp |
| `src/Aura.Application/Models/ConnectorExecutionResult.cs` | Modified | Expose max-processed-item timestamp |
| `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` | Modified | Read → execute → persist policy |
| `src/Aura.Infrastructure/Adapters/Ingestion/InMemoryIngestionCheckpointStore.cs` | Modified | Store new field shape |
| `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` | Modified | Persistence + idempotency + policy cases |
| `tests/Aura.ArchitectureTests/IngestionArchitectureTests.cs` | Verified | No SDK/Infrastructure leak into Application |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Partial-failure run wrongly advances finished-at | High | finished-at advances only on full success; partial runs advance max-processed-at only; unit-test the mixed outcome |
| Max-processed timestamp needs per-item data not in result | High | Add max-timestamp to canonical result; adapter computes it from successfully processed items only |
| Persistence policy leaks into Workers | Med | Keep all write policy in Application use case |

## Rollback Plan

Revert the use case to read-only and restore the single-timestamp `IngestionCheckpoint`/`ConnectorExecutionResult` shapes. No durable store or schema migration is involved (in-memory adapter), so rollback is code-only.

## Dependencies

- Existing `IIngestionCheckpointStore` port and `InMemoryIngestionCheckpointStore`.

## Success Criteria

- [ ] Full-success+new-items advances both timestamps; full-success+empty advances only finished-at; full-failure advances neither; mixed/partial-failure advances ONLY max-processed-at.
- [ ] Max-processed-at equals the highest timestamp among successfully processed items only (failed items excluded).
- [ ] A repeated run over the same window proves idempotent (no checkpoint regression).
- [ ] Architecture tests confirm no Infrastructure/SDK leakage into Application.
- [ ] `StoryBacklog.md` W2-H2-T3 marked complete at end of the full cycle (downstream rollout note).
