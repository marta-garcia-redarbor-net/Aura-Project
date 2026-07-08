# Tasks: W4-H1 Structured Logging with Correlation

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | ~357 |
| 400-line budget risk | Low |
| Chained PRs recommended | No |
| Suggested split | Single PR |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: No
Chain strategy: pending
400-line budget risk: Low

### Suggested Work Units

Not needed — single PR under 400 lines.

---

## Phase 1: Foundation (Ports & Services)

- [x] **T1** — `IErrorStore` port + `ErrorEntry` record
  - **DoD**: Interface compiles with `RecordAsync`/`GetRecentAsync`; record has `CorrelationId`, `Timestamp`, `Message`
  - **Files**: `src/Aura.Application/Ports/IErrorStore.cs` (new)
  - **Deps**: None
  - **Risk**: Low — pure contract
  - **Lines**: ~15

- [x] **T2** — `InMemoryErrorStore` (ring buffer, cap 100) + DI registration
  - **DoD**: Thread-safe store records up to cap, returns recent list, registered as singleton `IErrorStore` in `DependencyInjection.cs`
  - **Files**: `src/Aura.Infrastructure/Adapters/Services/InMemoryErrorStore.cs` (new), `src/Aura.Infrastructure/DependencyInjection.cs` (mod)
  - **Deps**: T1
  - **Risk**: Low — no external I/O
  - **Lines**: ~45

## Phase 2: API Correlation Middleware

- [x] **T3** — `CorrelationMiddleware` + register in Program.cs
  - **DoD**: Forwards/generates `X-Correlation-Id`, sets `TraceIdentifier`, opens `BeginScope`, logs entry+exit (method/path/status/ms), adds response header. Registered before `UseAuthentication`.
  - **Files**: `src/Aura.Api/Middleware/CorrelationMiddleware.cs` (new), `src/Aura.Api/Program.cs` (mod)
  - **Deps**: None
  - **Risk**: Low — standard ASP.NET middleware
  - **Lines**: ~50

## Phase 3: Worker Correlation Scope

- [x] **T4** — `CorrelatedWorkerBase` + integrate ConnectorExecutionWorker + SemanticIndexSyncWorker
  - **DoD**: Base class generates per-cycle GUID + `BeginScope` + calls `ExecuteCorrelatedAsync`. Both workers opt-in via inheritance. Polling logic preserved. **HelloKernelWorker excluded** per decision.
  - **Files**: `src/Aura.Workers/CorrelatedWorkerBase.cs` (new), `src/Aura.Workers/ConnectorExecutionWorker.cs` (mod), `src/Aura.Workers/SemanticIndexSyncWorker.cs` (mod)
  - **Deps**: None
  - **Risk**: Medium — ConnectorExecutionWorker has scope factory nesting, verify BeginScope placement
  - **Lines**: ~75

## Phase 4: LoggerMessage Migration

- [x] **T5** — Replace `_logger.Log*()` with `[LoggerMessage]` in 4 files
  - **DoD**: Zero `_logger.Log*()` remain in HelloKernelWorker (4 calls), Worker.cs (1), SemanticIndexSyncWorker (5 remaining), PluginRegistry (1). Output identical in level/template/params.
  - **Files**: `src/Aura.Workers/HelloKernelWorker.cs` (mod), `src/Aura.Workers/Worker.cs` (mod), `src/Aura.Workers/SemanticIndexSyncWorker.cs` (mod), `src/Aura.Application/Kernel/PluginRegistry.cs` (mod)
  - **Deps**: None
  - **Risk**: Low — compile-time only
  - **Lines**: ~65

## Phase 5: Dashboard Error Panel

- [x] **T6** — Add `GET /api/dashboard/recent-errors` endpoint
  - **DoD**: Returns `ErrorEntry[]` (200), empty list if none, follows dashboard group auth
  - **Files**: `src/Aura.Api/Endpoints/DashboardEndpoints.cs` (mod)
  - **Deps**: T2
  - **Risk**: Low — read-only additive endpoint
  - **Lines**: ~15

- [x] **T7** — UI: add `GetRecentErrorsAsync` to `SystemStatusApiClient` + render errors in `SystemStatusPanel.razor`
  - **DoD**: UI loads errors on init, displays correlation ID + timestamp + message
  - **Files**: `src/Aura.UI/Services/SystemStatusApiClient.cs` (mod), `src/Aura.UI/Components/Dashboard/SystemStatusPanel.razor` (mod)
  - **Deps**: T6
  - **Risk**: Low — UI-only, API contract verified in T6
  - **Lines**: ~30

## Phase 6: Testing

- [x] **T8** — Unit + integration tests for all components
  - **DoD**: Unit: `CorrelationMiddleware` (mock HttpContext/ILogger, verify scope + headers), `InMemoryErrorStore` (record/retrieve/capacity/empty). Integration: middleware pipeline via `WebApplicationFactory`, `CorrelatedWorkerBase` via hosted-service harness, `GET /api/dashboard/recent-errors`. All [LoggerMessage] migrations compile-checked.
  - **Files**: `tests/Aura.UnitTests/**`, `tests/Aura.IntegrationTests/**`
  - **Deps**: T1–T7
  - **Risk**: Medium — integration tests need `WebApplicationFactory` setup
  - **Lines**: ~100
