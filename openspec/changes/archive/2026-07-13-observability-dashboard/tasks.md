# Tasks: Observability Dashboard

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 900–1200 (13 new files + 2 modified + tests) |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 → PR 2 → PR 3 |
| Delivery strategy | ask-on-risk |
| Chain strategy | pending |

Decision needed before apply: Yes
Chained PRs recommended: Yes
Chain strategy: pending
400-line budget risk: High

### Suggested Work Units

| Unit | Goal | Likely PR | Notes |
|------|------|-----------|-------|
| 1 | Ring buffers + DTOs + listeners + DI extension (Infrastructure) | PR 1 | Foundation; unit-testable in isolation; no external deps |
| 2 | TelemetryHub + StreamService + API wiring | PR 2 | Depends on PR 1; integration-testable with HubConnection |
| 3 | Observability.razor + TelemetryClient + UI wiring | PR 3 | Depends on PR 2; end-to-end verification |

## Phase 1: Infrastructure — Ring Buffers & DTOs

- [x] 1.1 Create `src/Aura.Infrastructure/Observability/Dtos.cs` with `LogRecordDto`, `SpanDto`, `MetricSnapshotDto` records
- [x] 1.2 Create `src/Aura.Infrastructure/Observability/TelemetryBuffer.cs` — generic `TelemetryBuffer<T>` with `ConcurrentQueue`, bounded capacity, `Write()`, `Snapshot()`, `Count`
- [x] 1.3 Create `src/Aura.Infrastructure/Observability/LogRecordBuffer.cs`, `SpanBuffer.cs`, `MetricSnapshotBuffer.cs` — specialized subclasses with default capacities (1000, 500, 100)
- [x] 1.4 Write unit tests for `TelemetryBuffer<T>`: eviction on overflow, concurrent producers (10 threads × 100 writes), snapshot returns copy

## Phase 2: Infrastructure — Listeners & DI

- [x] 2.1 Create `src/Aura.Infrastructure/Observability/TelemetryLoggerProvider.cs` — `ILoggerProvider` that writes `LogRecordDto` to `LogRecordBuffer`, extracts CorrelationId from scope state
- [x] 2.2 Create `src/Aura.Infrastructure/Observability/TelemetryActivityListener.cs` — wraps `ActivityListener`, captures `SpanDto` on `ActivityStopped` with operation name, duration, status, tags
- [x] 2.3 Create `src/Aura.Infrastructure/Observability/TelemetryMeterListener.cs` — wraps `MeterListener`, captures `MetricSnapshotDto` on counter measurements for int/long/double
- [x] 2.4 Create `src/Aura.Infrastructure/Observability/ObservabilityExtensions.cs` — `AddAuraObservability()` registers buffers as singletons, listeners, and logger provider
- [x] 2.5 Write unit tests: logger provider captures log with CorrelationId; ActivityListener captures span with duration/tags; MeterListener captures counter measurement

## Phase 3: API — SignalR Hub & Background Service

- [x] 3.1 Create `src/Aura.Api/Hubs/TelemetryHub.cs` — `[Authorize]` hub with `StreamLogs`, `StreamMetrics`, `StreamTraces` returning `IAsyncEnumerable<T>` with 1s delay
- [x] 3.2 Create `src/Aura.Api/Services/TelemetryStreamService.cs` — `BackgroundService` polling buffers every 1s, pushing via `IHubContext<TelemetryHub>.Clients.All.SendAsync`
- [x] 3.3 Modify `src/Aura.Api/Program.cs` — add `services.AddAuraObservability()`, `services.AddHostedService<TelemetryStreamService>()`, `app.MapHub<TelemetryHub>("/hubs/telemetry")`
- [x] 3.4 Write integration test: connect `HubConnection` to test server, verify `ReceiveLogs`/`ReceiveMetrics`/`ReceiveTraces` arrive within 1s

## Phase 4: UI — Blazor Page & SignalR Client

- [x] 4.1 Create `src/Aura.UI/Services/TelemetryClient.cs` — `HubConnection` wrapper with `WithAutomaticReconnect`, events for `LogsReceived`/`MetricsReceived`/`TracesReceived`, `StartAsync()`, `IAsyncDisposable`
- [x] 4.2 Create `src/Aura.UI/Pages/Observability.razor` — `@page "/observability"` with `<AuthorizeView>`, three panels: log table (level-colored rows, virtual scroll container), metrics gauges grid, trace table with expandable tags
- [x] 4.3 Modify `src/Aura.UI/Program.cs` — register `TelemetryClient` as scoped service
- [x] 4.4 Write bUnit test: render `Observability.razor`, verify three panels render, verify log rows color-coded by level

## Phase 5: Verification & Cleanup

- [x] 5.1 Run `dotnet build Aura.sln` — verify zero errors
- [x] 5.2 Run `dotnet test Aura.sln` — verify all new and existing tests pass
- [ ] 5.3 Manual E2E: navigate to `/observability`, verify logs stream, metrics update, traces appear with tags
