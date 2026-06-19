# Tasks: W2-H2-T2 — Teams-First Connector Execution Flow

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 430-560 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 (Application contracts/use case/tests) → PR 2 (Teams adapter/worker/arch tests) |
| Delivery strategy | ask-always |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Application contracts + lightweight adapter-dispatch use case + RED/GREEN unit tests | PR 1 | Includes telemetry assertions; autonomous and reviewable |
| 2 | Teams adapter + Infrastructure/Workers wiring + architecture tests | PR 2 | Depends on Unit 1 contracts; keep one-shot worker and `teams` canonical name |

## Phase 1: Foundation / TDD RED

- [x] 1.1 [RED] Create `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` for spec scenarios: valid connector returns unchanged result, unregistered connector returns typed failure, checkpoint present/absent sets window.
- [x] 1.2 [RED] In `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs`, add telemetry assertions: success emits trace+metric+info log with one correlation ID; failure emits trace+metric(0)+error log with same ID.
- [x] 1.3 Create `src/Aura.Application/Ports/IConnectorAdapter.cs`, `src/Aura.Application/Models/ConnectorExecutionRequest.cs`, and `src/Aura.Application/Models/ConnectorExecutionResult.cs` (with status + failure reason contract used by tests).

## Phase 2: Core Implementation / TDD GREEN

- [x] 2.1 Implement `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` to read `IIngestionCheckpointStore` only, derive `WindowStart/WindowEnd`, and avoid any checkpoint writes (W2-H2-T3 deferred).
- [x] 2.2 In `ExecuteConnectorUseCase`, implement lightweight Strategy Pattern dispatch by selecting from `IEnumerable<IConnectorAdapter>` with `ConnectorName` match; do not add resolver ceremony.
- [x] 2.3 In `ExecuteConnectorUseCase`, return typed failure for unregistered adapter or adapter failure without re-throwing exceptions, preserving canonical result shape.
- [x] 2.4 Update `src/Aura.Application/DependencyInjection.cs` to register `ExecuteConnectorUseCase` and dependencies needed by current RED tests.

## Phase 3: Integration / Teams-First Wiring

- [x] 3.1 Create `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs` implementing one `IConnectorAdapter` for provider `teams`, keeping `Microsoft.Graph` types confined to Infrastructure and returning stub item count only.
- [x] 3.2 Create `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs` and update `src/Aura.Infrastructure/DependencyInjection.cs` to register `TeamsConnectorAdapter` as `IConnectorAdapter`.
- [x] 3.3 Create `src/Aura.Workers/ConnectorExecutionWorker.cs` as one-shot host orchestration and update `src/Aura.Workers/Program.cs` to add hosted service in the full-mode branch.

## Phase 4: Verification / REFACTOR

- [x] 4.1 Create `tests/Aura.ArchitectureTests/ConnectorExecutionArchitectureTests.cs` to assert `IConnectorAdapter` resides in `Aura.Application.Ports` and `Aura.Application` has no `Microsoft.Graph` dependency.
- [x] 4.2 Refactor `ExecuteConnectorUseCase` and `TeamsConnectorAdapter` logging to `LoggerMessage` partials while preserving shared correlation identifier across trace/metric/log.
- [x] 4.3 Run `dotnet test Aura.sln` and stabilize assertions for all connector-execution scenarios from `openspec/changes/W2-H2-T2/specs/connector-execution/spec.md`.

## Phase 5: Cleanup / Documentation

- [x] 5.1 Add concise comments in `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` and `src/Aura.Workers/ConnectorExecutionWorker.cs` documenting lightweight strategy dispatch, one-shot execution, and read-only checkpoint boundary.
- [x] 5.2 Keep `openspec/changes/W2-H2-T2/tasks.md` checklist state aligned with implementation progress during `sdd-apply` (no scope expansion to mapping, persistence, or other connectors).
