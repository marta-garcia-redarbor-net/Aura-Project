# Tasks: W2-H2-T3 — Ingestion Checkpoint Persistence

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 280-360 |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR (2 work units / commits) |
| Delivery strategy | ask-always (risk-gated decision) |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Models + checkpoint shape + RED tests | Single PR | Commit with model updates and checkpoint-contract tests together |
| 2 | Use-case policy + telemetry + scenario verification | Single PR | Commit with policy logic, unit scenarios, and architecture verification |

## Phase 1: Foundation (Model Contracts)

- [x] 1.1 RED: Extend `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` for three-field checkpoint shape and independent null preservation scenarios.
- [x] 1.2 GREEN: Update `src/Aura.Application/Models/IngestionCheckpoint.cs` to `Cursor`, `MaxProcessedAt`, `ExecutionFinishedAt` (rename from `ProcessedAt`).
- [x] 1.3 REFACTOR: Update checkpoint constructor call sites in `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` and `tests/Aura.UnitTests/Ingestion/IngestionCheckpointFirstRunWindowTests.cs`.

## Phase 2: Core Implementation (Execution Result + Persistence Policy)

- [x] 2.1 RED: Add failing scenarios in `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` for full-success+items, full-success+empty, full-failure, partial-failure, and idempotent re-run.
- [x] 2.2 GREEN: Extend `src/Aura.Application/Models/ConnectorExecutionResult.cs` with `MaxProcessedAt` and add `PartialFailure` to `ConnectorExecutionStatus`.
- [x] 2.3 GREEN: Modify `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` to read `MaxProcessedAt` window start and persist checkpoints via private `PersistCheckpointAsync(...)` using the four-outcome policy.
- [x] 2.4 REFACTOR: Update telemetry/log tagging and success/failure logging paths in `ExecuteConnectorUseCase.cs` to handle `PartialFailure` without leaking provider SDK types.

## Phase 3: Integration / Architecture Guard

- [x] 3.1 Update `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` assertions to verify exact round-trip values for both timestamps and cursor across identities.
- [x] 3.2 Verify `src/Aura.Infrastructure/Adapters/Ingestion/InMemoryIngestionCheckpointStore.cs` remains contract-compatible with the new record shape (no provider concerns, no port change).
- [x] 3.3 Validate clean-boundary compliance by executing `tests/Aura.ArchitectureTests/IngestionArchitectureTests.cs` after Application model/use-case changes.

## Phase 4: Verification (Spec Scenario Coverage)

- [x] 4.1 Run targeted tests: `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` and `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs`; confirm all modified spec scenarios pass.
- [x] 4.2 Run full project verification command `dotnet test Aura.sln` and capture failures tied to checkpoint policy regressions before closure.

## Phase 5: Cleanup / Completion

- [x] 5.1 Update `openspec/changes/W2-H2-T3/tasks.md` checkboxes during apply to reflect completed work units.
- [x] 5.2 After full implementation+verify+archive, mark `W2-H2-T3` as complete in `StoryBacklog.md`.
