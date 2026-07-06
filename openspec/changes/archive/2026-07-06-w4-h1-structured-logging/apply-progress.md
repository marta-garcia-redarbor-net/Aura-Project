# SDD Apply Progress: w4-h1-structured-logging

## Scope
Verify-blocker remediation only (strict TDD batch).

## Cumulative Task Status (Merged)

### Previous completed tasks (from prior apply-progress)
- [x] **T1** — `IErrorStore` port + `ErrorEntry` record
- [x] **T2** — `InMemoryErrorStore` ring buffer + DI registration
- [x] **T3** — API `CorrelationMiddleware` + registration in `Program.cs`
- [x] **T4** — `CorrelatedWorkerBase` + worker integration
- [x] **T5** — `[LoggerMessage]` migration in target files
- [x] **T6** — `GET /api/dashboard/recent-errors` endpoint
- [x] **T7** — UI errors rendering in `SystemStatusPanel`
- [x] **T8** — unit/integration test suite for initial slice

### Verify remediation tasks (this batch)
- [x] **VR1** — Add missing OpenSpec apply-progress artifact with strict TDD evidence
- [x] **VR2** — Ensure runtime writes `ErrorEntry` to `IErrorStore`
- [x] **VR3** — Ensure `ExecuteConnectorUseCase` opens `BeginScope` before adapter execution
- [x] **VR4** — Close runtime/spec evidence gaps (entry/exit logs + duration, downstream scope propagation, worker scope propagation, logger-message runtime parity, Graph telemetry structured evidence)

## TDD Cycle Evidence
| Task | Test File | Layer | Safety Net | RED | GREEN | TRIANGULATE | REFACTOR |
|------|-----------|-------|------------|-----|-------|-------------|----------|
| VR1 | `openspec/changes/w4-h1-structured-logging/apply-progress.md` | Artifact | N/A | ✅ Written | ✅ Saved | ➖ Single | ➖ None needed |
| VR2 | `tests/Aura.IntegrationTests/Middleware/CorrelationMiddlewarePipelineTests.cs` | Integration | ✅ targeted baseline run | ✅ Added failing `DashboardException_RecordsErrorEntryUsingRequestCorrelationId` first | ✅ Pass after recording `ErrorEntry` in dashboard pipeline on exception/5xx | ✅ Added 5xx branch coverage in same flow | ✅ Kept middleware behavior focused, no new deps |
| VR3 | `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs` | Unit | ✅ targeted baseline run | ✅ Added failing `ExecuteAsync_OpensCorrelationScope_BeforeAdapterExecution` first | ✅ Pass after adding `BeginScope("{CorrelationId}", ...)` before adapter call | ✅ Correlated scope value asserted against emitted success log | ✅ Minimal signature update in telemetry helper only |
| VR4-A | `tests/Aura.UnitTests/Middleware/CorrelationMiddlewareTests.cs` | Unit | ✅ targeted baseline run | ✅ Added failing entry/exit + downstream-scope tests first | ✅ Pass with runtime log/state assertions | ✅ Non-empty behavior paths (method/path/status/duration + downstream log scope) | ✅ Shared scope-aware logger test double extracted |
| VR4-B | `tests/Aura.UnitTests/Workers/CorrelatedWorkerBaseTests.cs` | Unit | ✅ targeted baseline run | ✅ Added worker-scope propagation test first | ✅ Pass with scoped correlation assertion on worker log | ✅ Existing per-cycle GUID tests + new scope propagation assertion | ✅ No production changes required |
| VR4-C | `tests/Aura.UnitTests/Infrastructure/LoggerMessageParityTests.cs` | Unit | ✅ targeted baseline run | ✅ Replaced compile-smoke checks with runtime parity tests | ✅ Pass asserting event IDs, levels, structured params across migrated files | ✅ Covers Worker, HelloKernelWorker, PluginRegistry, SemanticIndexSyncWorker | ✅ Removed obsolete compile-only test file |
| VR4-D | `tests/Aura.UnitTests/GraphConnector/*.cs`, `tests/Aura.UnitTests/Ingestion/Calendar/GraphCalendarEventProviderTests.cs` | Unit | ✅ targeted baseline run | ✅ Added failing telemetry assertions for MSAL/HTTP 4xx/5xx | ✅ Pass after provider log+metric remediation | ✅ 4xx warning + 5xx error + token-expired scenarios across Teams/Outlook/Calendar | ✅ Introduced reusable `MeterCapture` helper for deterministic metric assertions |
| VR4-E | `tests/Aura.IntegrationTests/Middleware/CorrelationMiddlewarePipelineTests.cs` | Integration | ✅ targeted baseline run | ✅ Added failing request-log payload test first | ✅ Pass with scoped request log assertions (method/path/status/duration + correlation) | ✅ Coupled header-derived correlation with captured logger scope | ✅ Logger provider test double kept local to integration test |

## Test Summary (this remediation batch)
- **Targeted unit tests**: 72 passed, 0 failed
- **Targeted integration tests**: 12 passed, 0 failed
- **Layers used**: Unit, Integration
- **Approval tests**: None
- **Pure functions created**: 0

## Files Touched in This Batch
- `src/Aura.Api/Program.cs`
- `src/Aura.Application/UseCases/ConnectorExecution/ExecuteConnectorUseCase.cs`
- `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphTeamsSourceProvider.cs`
- `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphOutlookSourceProvider.cs`
- `src/Aura.Infrastructure/Adapters/Connectors/Calendar/GraphCalendarEventProvider.cs`
- `tests/Aura.UnitTests/Middleware/CorrelationMiddlewareTests.cs`
- `tests/Aura.UnitTests/Ingestion/ExecuteConnectorUseCaseTests.cs`
- `tests/Aura.UnitTests/Workers/CorrelatedWorkerBaseTests.cs`
- `tests/Aura.UnitTests/Infrastructure/LoggerMessageParityTests.cs` (new)
- `tests/Aura.UnitTests/Infrastructure/LoggerMessageCompileCheckTests.cs` (removed)
- `tests/Aura.UnitTests/GraphConnector/GraphTeamsSourceProviderTests.cs`
- `tests/Aura.UnitTests/GraphConnector/GraphOutlookSourceProviderTests.cs`
- `tests/Aura.UnitTests/Ingestion/Calendar/GraphCalendarEventProviderTests.cs`
- `tests/Aura.UnitTests/TestDoubles/Observability/ScopeAwareTestLogger.cs` (new)
- `tests/Aura.UnitTests/TestDoubles/Observability/MeterCapture.cs` (new)
- `tests/Aura.IntegrationTests/Middleware/CorrelationMiddlewarePipelineTests.cs`

## Notes
- Scope intentionally limited to verify blockers.
- No external dependencies added.
- Clean Architecture boundaries preserved.
