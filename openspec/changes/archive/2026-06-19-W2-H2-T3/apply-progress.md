# Apply Progress: W2-H2-T3 — Ingestion Checkpoint Persistence

## Execution Mode

- **Mode**: Strict TDD
- **Test runner**: `dotnet test`
- **Delivery decision**: single PR approved within forecast (280–360 lines, medium risk)

## Completed Tasks

- [x] 1.1 RED: Extended checkpoint store contract tests for 3-field shape and null-preservation scenarios.
- [x] 1.2 GREEN: Updated `IngestionCheckpoint` to `Cursor`, `MaxProcessedAt`, `ExecutionFinishedAt`.
- [x] 1.3 REFACTOR: Updated checkpoint constructor call sites in ingestion test suites.
- [x] 2.1 RED: Added five failing use-case policy scenarios (full-success+items, full-success+empty, full-failure, partial-failure, idempotent rerun).
- [x] 2.2 GREEN: Extended `ConnectorExecutionResult` with `MaxProcessedAt` and added `PartialFailure` status.
- [x] 2.3 GREEN: Implemented checkpoint persistence policy in `ExecuteConnectorUseCase` via `PersistCheckpointAsync` using four-outcome rules.
- [x] 2.4 REFACTOR: Updated telemetry and logging to support `PartialFailure` without SDK leakage.
- [x] 3.1 Updated store contract assertions for exact round-trip of cursor + both timestamps.
- [x] 3.2 Verified in-memory checkpoint store remains contract-compatible with new record shape.
- [x] 3.3 Executed ingestion architecture guard tests to confirm clean boundaries.
- [x] 4.1 Ran targeted ingestion tests for updated policy and store contracts.
- [x] 4.2 Ran full solution verification (`dotnet test Aura.sln`).
- [x] 5.1 Updated `tasks.md` checkboxes during apply.
- [x] 5.2 Marked `W2-H2-T3` as complete in `StoryBacklog.md` during final closure pass.

## Remediation Pass (Post-Verify FAIL)

- [x] Added direct runtime assertions for canonical `ConnectorExecutionResult` contract in all four required scenarios:
  - full success with items
  - full success with no items
  - full failure
  - partial failure
- [x] Added a narrow exception-branch test for `ExecuteConnectorUseCase` (`adapter throws` → typed failure contract).
- [x] Confirmed worker-level runtime semantics were not changed in this remediation scope; no speculative worker tests were added.

## TDD Cycle Evidence

| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| 1.1 | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | Unit | ✅ `ExecuteConnectorUseCaseTests` + ingestion baseline (25/25) | ✅ Added 3-field/null scenarios first (compile failed) | ✅ `dotnet test ...InMemoryCheckpointStoreContractTests|CheckpointIdentityTests|IngestionCheckpointFirstRunWindowTests` (18/18) | ✅ Both non-null and null timestamp paths covered | ✅ Assertions normalized to explicit fields |
| 1.2 | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | Unit | ✅ same baseline | ✅ Tests referenced `MaxProcessedAt`/`ExecutionFinishedAt` before model update | ✅ same targeted run passed after model update | ✅ Multiple timestamp combinations validated | ➖ None needed |
| 1.3 | `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs`, `tests/Aura.UnitTests/Ingestion/IngestionCheckpointFirstRunWindowTests.cs` | Unit | ✅ same baseline | ✅ Existing constructors intentionally broke after model change | ✅ same targeted run passed after call-site updates | ✅ Present/absent checkpoint paths still pass | ✅ Constructor call sites cleaned and aligned |
| 2.1 | `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` | Unit | ✅ `dotnet test ...ExecuteConnectorUseCaseTests` baseline | ✅ Five policy tests written first (compile failed on missing result/status fields) | ✅ `dotnet test ...ExecuteConnectorUseCaseTests` (13/13) | ✅ Success, empty, failure, partial, rerun scenarios all covered | ✅ Capturing/persistence assertions consolidated |
| 2.2 | `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` | Unit | ✅ same baseline | ✅ New tests referenced `PartialFailure` and `MaxProcessedAt` before model support | ✅ same targeted run passed | ✅ Partial vs success/failure paths assert distinct behavior | ➖ None needed |
| 2.3 | `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` | Unit | ✅ same baseline | ✅ Save-policy tests failed before `PersistCheckpointAsync` | ✅ same targeted run passed after policy implementation | ✅ All four outcome policies + idempotency covered | ✅ Policy isolated in private helper |
| 2.4 | `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` | Unit | ✅ same baseline | ✅ Partial-failure telemetry/log behavior asserted first | ✅ same targeted run passed | ✅ Failure vs partial vs success telemetry/log branches covered | ✅ Added explicit partial-failure log event |
| 3.1 | `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | Unit | ✅ same baseline | ✅ Exact round-trip field assertions written before pass | ✅ targeted ingestion contract run passed | ✅ Distinct identities + null-preservation covered | ✅ Removed ambiguous equality-only checks |
| 3.2 | `src/Aura.Infrastructure/Adapters/Ingestion/InMemoryIngestionCheckpointStore.cs` (verified via tests) | Unit | ✅ same baseline | ✅ Contract tests exercised adapter with new shape first | ✅ targeted ingestion contract run passed | ✅ Multiple shape combinations persisted/retrieved | ➖ None needed |
| 3.3 | `tests/Aura.ArchitectureTests/IngestionArchitectureTests.cs` | Architecture | ✅ architecture baseline from prior targeted run | ✅ Existing guard reused as approval test after model/use-case updates | ✅ `dotnet test ...IngestionArchitectureTests` (2/2) | ✅ Port placement + dependency boundaries validated | ➖ None needed |
| 4.1 | `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs`, `tests/Aura.UnitTests/Ingestion/InMemoryCheckpointStoreContractTests.cs` | Unit | ✅ N/A (verification step) | ✅ Scenarios already introduced in RED cycle | ✅ `dotnet test ...ExecuteConnectorUseCaseTests|InMemoryCheckpointStoreContractTests` (20/20) | ✅ All modified spec scenarios represented | ➖ None needed |
| 4.2 | `Aura.sln` | Unit + Integration + Architecture + E2E | ✅ N/A (verification step) | ✅ Verification command pre-defined | ✅ `dotnet test Aura.sln` passed (383/383) | ✅ Full-suite confirms no cross-layer regressions | ➖ None needed |
| 5.1 | `openspec/changes/W2-H2-T3/tasks.md` | Process | N/A | ✅ Checklist updates done as tasks completed | ✅ Re-read confirms completed tasks are `[x]` | ➖ Single path | ➖ None needed |
| 5.2 | `StoryBacklog.md`, `openspec/changes/W2-H2-T3/tasks.md` | Process | N/A | ✅ Closure pass identified remaining unchecked story/task | ✅ Updated backlog story status and task 5.2 to `[x]` | ➖ Single closure path | ➖ None needed |
| R1 | `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` | Unit | ✅ `dotnet test ...ExecuteConnectorUseCaseTests` baseline (13/13) | ✅ Added four direct canonical-result contract tests first | ✅ `dotnet test ...ExecuteConnectorUseCaseTests` (18/18) | ✅ Full-success(with/without items), full-failure, partial-failure all assert identity/item-count/status/reason/max-processed-at directly | ➖ None needed |
| R2 | `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` | Unit | ✅ same baseline | ✅ Added throw-path contract test first (`ExecuteAsync_WhenAdapterThrows_ReturnsTypedFailureContract`) | ✅ covered in same targeted run (18/18) | ✅ Distinguishes adapter-returned failure from exception-translated failure contract | ➖ None needed |

## Test Summary

- **Total tests written**: 11
- **Total tests passing**: 383 (full solution)
- **Layers used**: Unit (18 targeted ingestion tests + 1 targeted worker smoke), Architecture (2 targeted), Integration/E2E validated in full run
- **Approval tests (refactoring)**: Existing ingestion and architecture tests reused while refactoring telemetry/policy flow
- **Pure functions created**: 0 (use-case orchestration flow)

## Remediation Test Runs

- `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~Aura.UnitTests.Ingestion.ExecuteConnectorUseCaseTests"` → **18/18 passed**
- `dotnet test tests/Aura.UnitTests/Aura.UnitTests.csproj --filter "FullyQualifiedName~Aura.UnitTests.Workers.ConnectorExecutionWorkerTests"` → **1/1 passed**

## Remaining Work

- None. All `tasks.md` items are now complete (14/14).

## Notes

- Policy remains in `Application` (`ExecuteConnectorUseCase`) with no SDK leakage.
- Infrastructure connector (`TeamsConnectorAdapter`) now returns canonical `MaxProcessedAt` to support provider-agnostic policy evaluation.
- Final closure pass completed backlog synchronization: `StoryBacklog.md` now marks `W2-H2-T3` as done, matching `openspec` task state.
