# Proposal: Observability Dashboard

## Intent

Aura emits structured logs, traces (6 `ActivitySource`), and metrics (2 `Meter` with `Counter<T>`) across API, Application, Infrastructure, and Workers — but has **zero live visibility**. No exporters, no dashboard, no OpenTelemetry pipeline. For the upcoming tribunal demo, the team needs to prove observability works in real-time without external infrastructure.

## Scope

### In Scope
- **Log viewer**: Real-time table (level, timestamp, correlationId, message) inside Aura.UI
- **Metrics display**: Live values for connector execution items, Graph tokens/errors
- **Trace timeline**: Recent spans with duration and status
- **In-process ring buffers**: `ActivityListener` + `MeterListener` + `IObservable<LogRecord>` — no external storage
- **SignalR push**: New `TelemetryHub` for real-time streaming from API/Workers to UI
- **Route**: New `/observability` Blazor page
- **Clean Architecture**: Buffers and listeners in Infrastructure, page in UI, hub in API

### Out of Scope
- External exporters (OTLP, Prometheus, Grafana, Loki, Seq)
- Persistent telemetry storage
- Alerting or SLO tracking
- Historical search beyond ring-buffer capacity

## Capabilities

### New Capabilities
- `observability-dashboard`: Real-time telemetry viewer with logs, metrics, and traces inside Aura.UI

### Modified Capabilities
- `structured-logging`: Add in-process log subscription via `Microsoft.Extensions.Diagnostics.Buffering` or custom `IObserver<LogRecord>` bridge

## Approach

1. **Ring buffers**: Create `Aura.Infrastructure.Observability` with `TelemetryBuffer<T>` (bounded, thread-safe) fed by `ActivityListener` and `MeterListener`. Subscribe to log records via `ILoggerProvider`→`IObservable<LogRecord>` pattern.
2. **TelemetryHub** (`Aura.Api.Hubs`): New SignalR hub streaming buffer snapshots on timer + push on new data.
3. **Blazor page** (`Aura.UI/Pages/Observability.razor`): Three panels — log table (virtual scroll), metrics gauges, trace timeline — all fed by SignalR client.
4. **No NuGet additions**: All APIs used (`ActivityListener`, `MeterListener`, SignalR, `IObservable<T>`) exist in current .NET 9 / ASP.NET Core.
5. **Auth reuse**: Existing `AuthorizeView` + cookie/OIDC pattern applies to `/observability`.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Observability/` | New | Ring buffers + listener registration |
| `src/Aura.Api/Hubs/TelemetryHub.cs` | New | SignalR hub for telemetry streaming |
| `src/Aura.Api/Program.cs` | Modified | Register listeners + map hub |
| `src/Aura.UI/Pages/Observability.razor` | New | Three-panel dashboard page |
| `src/Aura.UI/Services/TelemetryClient.cs` | New | SignalR client wrapper |
| `src/Aura.UI/Program.cs` | Modified | Register telemetry client + route |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Performance: listener callbacks block hot paths | Low | Use unbounded `Channel<T>` + batch flush; never await in listeners |
| Ring-buffer memory ceiling | Low | Fixed capacity (e.g. 1000 logs, 500 spans, 100 metric snapshots) |
| SignalR circuit reconnection | Low | Auto-reconnect with backoff in client; full re-subscribe on reconnect |

## Rollback Plan

Revert changes across 4 areas: remove `TelemetryHub` registration and `MapHub` in API `Program.cs`, remove `TelemetryClient` registration in UI `Program.cs`, delete the three new files. No config changes, no database, no schema migrations.

## Dependencies

- .NET 9 built-in `System.Diagnostics.ActivityListener` and `System.Diagnostics.Metrics.MeterListener`
- Existing SignalR infrastructure (`AddSignalR`, `AlertHub`, Blazor Server circuit)

## Success Criteria

- [ ] `/observability` page loads with three panels (logs, metrics, traces)
- [ ] Log panel streams live entries with level, timestamp, correlationId, and message
- [ ] Metrics panel shows current values for `aura.connector.execution.items`, `graph.http.error`, etc.
- [ ] Trace panel shows recent spans with operation name, duration, and status
- [ ] SignalR push delivers data within 1 second of emission
- [ ] `dotnet build Aura.sln` succeeds
- [ ] `dotnet test Aura.sln` passes
