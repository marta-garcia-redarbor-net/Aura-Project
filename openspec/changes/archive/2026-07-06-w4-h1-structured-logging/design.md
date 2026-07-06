# Design: W4-H1 Structured Logging with Correlation

## Technical Approach

Add a lightweight correlation ID layer across both hosts (API + Workers) with zero new NuGet dependencies. The API gets an ASP.NET Core middleware class for `X-Correlation-Id` forwarding/generation + `ILogger.BeginScope`. Workers get an opt-in abstract base class that wraps each execution cycle with a correlation scope. Four remaining `_logger.Log*()` call sites migrate to `[LoggerMessage]`. Dashboard gets an in-memory error ring buffer exposed via a new read-only endpoint.

## Architecture Decisions

| Decision | Choice | Alternatives | Rationale |
|----------|--------|-------------|-----------|
| Middleware form | Class middleware (`CorrelationMiddleware`) | Inline `app.Use(...)` | Testable via `WebApplicationFactory`, follows ASP.NET conventions, no extra DI setup |
| Middleware location | `src/Aura.Api/Middleware/` | `Infrastructure` | Host-specific concern — middleware depends on `HttpContext`, breaks Clean Architecture if placed in Infrastructure |
| Worker correlation | `CorrelatedWorkerBase : BackgroundService` | Interface + extension method | Opt-in by inheritance preserves existing workers; abstract `ExecuteCorrelatedAsync` is explicit |
| Error store | `InMemoryErrorStore` (singleton, ring buffer) | DB-backed store, `ILogger` sink | No DB migration needed; errors are ephemeral dashboard state, process-scoped |
| Error store port | `IErrorStore` in `Aura.Application.Ports` | Inline in Api | Follows existing ports pattern; keeps Api agnostic of storage implementation |
| Dashboard error endpoint | Add to existing `DashboardEndpoints.cs` | New file | Reuses `/api/dashboard` group auth, minimal diff |

## Data Flow

```
API Request                            Worker Cycle
─────────────────                     ─────────────
Client ──→ CorrelationMiddleware       CorrelatedWorkerBase
  │         ├─ Read/generate ID          ├─ Generate GUID
  │         ├─ HttpContext.TraceId=id    ├─ BeginScope({CorrelationId})
  │         ├─ BeginScope({CorrelationId}) └─ ExecuteCorrelatedAsync()
  │         ├─ Log entry (method,path)       │
  │         ├─ await next()                  ├─ UseCase.Execute()
  │         ├─ Log exit (status,ms)          ├─ _logger.Log* (auto scope)
  │         └─ Response header X-Correlation-Id └─ scope disposed
  │                                           
Error path: dashboard middleware catch
  └─ InMemoryErrorStore.RecordAsync() ←── exception + correlationId
       │
       └─ GET /api/dashboard/recent-errors ─→ ErrorEntry[]
```

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Api/Middleware/CorrelationMiddleware.cs` | Create | ASP.NET middleware: read/generate X-Correlation-Id, BeginScope, log entry/exit, response header |
| `src/Aura.Workers/CorrelatedWorkerBase.cs` | Create | Abstract BackgroundService — generates Guid + BeginScope per cycle, child overrides `ExecuteCorrelatedAsync` |
| `src/Aura.Api/Program.cs` | Modify | Add `app.UseMiddleware<CorrelationMiddleware>()` before `UseAuthentication` (line 49) |
| `src/Aura.Api/Endpoints/DashboardEndpoints.cs` | Modify | Add `GET /api/dashboard/recent-errors` with `IErrorStore` DI, push errors from dashboard middleware catch |
| `src/Aura.Application/Ports/IErrorStore.cs` | Create | Port: `RecordAsync(ErrorEntry)` + `GetRecentAsync(int count)` |
| `src/Aura.Infrastructure/Services/InMemoryErrorStore.cs` | Create | Thread-safe ring buffer (capacity 100), registered as singleton in `AddAuraInfrastructure` |
| `src/Aura.Workers/SemanticIndexSyncWorker.cs` | Modify | Migrate 5 `_logger.Log*()` to `[LoggerMessage]` partial methods |
| `src/Aura.Workers/Worker.cs` | Modify | Migrate 1 `_logger.LogInformation()` to `[LoggerMessage]` |
| `src/Aura.Workers/HelloKernelWorker.cs` | Modify | Migrate 4 `_logger.Log*()` to `[LoggerMessage]` |
| `src/Aura.Application/Kernel/PluginRegistry.cs` | Modify | Migrate 1 `_logger.LogError()` to `[LoggerMessage]` |
| `src/Aura.Workers/ConnectorExecutionWorker.cs` | Modify | Inherit `CorrelatedWorkerBase`, add `BeginScope` per cycle (opt-in) |
| `src/Aura.Workers/SemanticIndexSyncWorker.cs` | Modify | Inherit `CorrelatedWorkerBase`, add `BeginScope` per cycle (opt-in) |

## Interfaces / Contracts

```csharp
// Aura.Application/Ports/IErrorStore.cs
public sealed record ErrorEntry(
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Message);

public interface IErrorStore
{
    Task RecordAsync(ErrorEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<ErrorEntry>> GetRecentAsync(int count, CancellationToken ct = default);
}
```

```csharp
// Aura.Workers/CorrelatedWorkerBase.cs
public abstract class CorrelatedWorkerBase : BackgroundService
{
    private readonly ILogger _logger;
    protected CorrelatedWorkerBase(ILogger logger) => _logger = logger;

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var correlationId = Guid.NewGuid().ToString();
            using var _ = _logger.BeginScope("{CorrelationId}", correlationId);
            await ExecuteCorrelatedAsync(correlationId, stoppingToken);
        }
    }

    protected abstract Task ExecuteCorrelatedAsync(
        string correlationId, CancellationToken stoppingToken);
}
```

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| Unit | `CorrelationMiddleware` logic (ID gen, header read, BeginScope) | Mock `HttpContext`, `ILogger`; inject known headers, verify scope + response headers |
| Unit | `InMemoryErrorStore` | Record + retrieve, verify capacity bound, empty list |
| Unit | All 4 `[LoggerMessage]` migrations | Compile check + log assertion via `ITestOutputHelper` |
| Integration | Correlation middleware in pipeline | `WebApplicationFactory` with test client, verify `X-Correlation-Id` on all responses |
| Integration | CorrelatedWorkerBase | Hosted service test with `BackgroundService` harness, verify scope |
| Integration | `GET /api/dashboard/recent-errors` | `WebApplicationFactory`, seed errors, verify response shape |

## Migration / Rollout

No migration required. All changes are additive:
- `CorrelationMiddleware` is new middleware — removing it leaves existing behavior intact
- `CorrelatedWorkerBase` is opt-in — existing `BackgroundService` workers unchanged
- `[LoggerMessage]` migrations are compile-time only — identical output
- `InMemoryErrorStore` is ephemeral — no data to migrate
- Rollback: revert `Program.cs` middleware registration, revert worker inheritance changes, keep `[LoggerMessage]` and error endpoint

## Open Questions

- [ ] `HelloKernelWorker`: should it also adopt `CorrelatedWorkerBase` or only the `[LoggerMessage]` migration? It's fire-once and already creates its own correlationId.
- [ ] Dashboard error panel UI: does the frontend already exist in the Blazor UI project, or is this just the API contract?
