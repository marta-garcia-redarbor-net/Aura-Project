# Tasks: W2-H3 — Teams Plugin Mapping and Work Item Persistence

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 180-320 |
| 400-line budget risk | Medium |
| Chained PRs recommended | No |
| Suggested split | Single PR (size-exception approved) |
| Delivery strategy | exception-ok |
| Chain strategy | size-exception |

Decision needed before apply: No
Chained PRs recommended: No
Chain strategy: size-exception
400-line budget risk: Medium

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Close strict-TDD evidence and missing RED cases | Single PR | Tests + `apply-progress.md` together |
| 2 | Fix DI lifetime, mapper traceability, and DI runtime proof | Single PR | Finish with focused + full verification |

## Phase 1: RED — Contracts and Boundary Tests

- [x] 1.1 Added failing architecture checks in `tests/Aura.ArchitectureTests/ConnectorExecutionArchitectureTests.cs` for port placement/Teams leakage.
- [x] 1.2 Added failing mapper cases in `tests/Aura.UnitTests/Ingestion/Teams/TeamsWorkItemMapperTests.cs`.
- [x] 1.3 Added failing handoff/persistence tests in `tests/Aura.UnitTests/Ingestion/Teams/TeamsConnectorAdapterTests.cs` and `tests/Aura.UnitTests/ConnectorExecution/ExecuteConnectorUseCaseWorkItemTests.cs`.
- [x] 1.4 Added failing store/buffer tests in `tests/Aura.UnitTests/WorkItems/InMemoryWorkItemStoreTests.cs` and `tests/Aura.UnitTests/WorkItems/InMemoryWorkItemBufferTests.cs`.

## Phase 2: GREEN — Ports, Mapper, Adapter, Store

- [x] 2.1 Created ports/models in `src/Aura.Application/Ports/*` and `src/Aura.Application/Models/WorkItemPersistenceResult.cs`.
- [x] 2.2 Created Teams mapping ACL in `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsMessageDto.cs` and `TeamsWorkItemMapper.cs`.
- [x] 2.3 Updated `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs` to map + enqueue only.
- [x] 2.4 Created in-memory buffer/store and DI module in `src/Aura.Infrastructure/Adapters/WorkItems/*`.

## Phase 3: GREEN — Use Case Wiring and DI Integration

- [x] 3.1 Updated `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs` to drain buffer and persist items.
- [x] 3.2 Upgraded use-case result to `PartialFailure` on any persistence failure.
- [x] 3.3 Updated `src/Aura.Infrastructure/Adapters/Connectors/DependencyInjection.cs` to register work-item dependencies.

## Phase 4: REFACTOR / Verification / Cleanup

- [x] 4.1 Refactored mapper/adapter internals while preserving rules and scenarios.
- [x] 4.2 Verified `SourceType`, batch-continue behavior, and failure-reason assertions.
- [x] 4.3 Ran `dotnet test Aura.sln` and fixed regressions.

## Phase 5: REMEDIATION RED — Verify Gaps

- [x] 5.1 Create `openspec/changes/W2-H3/apply-progress.md` with strict-TDD `TDD Cycle Evidence` for 14 completed + remediation tasks.
- [x] 5.2 Add failing mapper tests in `tests/Aura.UnitTests/Ingestion/Teams/TeamsWorkItemMapperTests.cs` for absent priority defaulting to Medium.
- [x] 5.3 Add failing mapper metadata tests in `tests/Aura.UnitTests/Ingestion/Teams/TeamsWorkItemMapperTests.cs` for defaulted `Title` and `Source` traceability.
- [x] 5.4 Add failing DI runtime tests in `tests/Aura.UnitTests/Infrastructure/InfrastructureDependencyInjectionTests.cs` for `IWorkItemStore` and `IWorkItemBuffer` scope isolation.
- [x] 5.5 Add failing tests for default fixture path in `tests/Aura.UnitTests/Ingestion/Teams/TeamsConnectorAdapterTests.cs` and empty failure-reason guard in `tests/Aura.UnitTests/WorkItems/InMemoryWorkItemStoreTests.cs` (or new `WorkItemPersistenceResult` tests).

## Phase 6: REMEDIATION GREEN — Implementation Fixes

- [x] 6.1 Update `src/Aura.Infrastructure/Adapters/WorkItems/DependencyInjection.cs` and connector registrations so `IWorkItemBuffer` is scoped/execution-local per design intent.
- [x] 6.2 Update `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsWorkItemMapper.cs` to record metadata for absent priority and defaulted `Title`/`Source` values.
- [x] 6.3 Implement 5.5 fixes in `src/Aura.Infrastructure/Adapters/Connectors/Teams/TeamsConnectorAdapter.cs` and persistence-result guard location for non-empty failure reasons.

## Phase 7: REMEDIATION Verification & Evidence

- [x] 7.1 Run focused tests for mapper, adapter, work-item, connector use-case, and DI runtime paths; log commands/results in `openspec/changes/W2-H3/apply-progress.md`.
- [x] 7.2 Run `dotnet test Aura.sln --collect:"XPlat Code Coverage"`; update ledger to GREEN/REFACTOR and add safety-net evidence for modified files.
