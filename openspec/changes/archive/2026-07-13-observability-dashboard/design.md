# Design: Observability Dashboard

## Technical Approach

Real-time telemetry viewer for Aura using in-process ring buffers, .NET 9 built-in listeners (`ActivityListener`, `MeterListener`), and SignalR streaming. Follows Clean Architecture: buffers/listeners in Infrastructure, hub in API, Blazor page in UI. No external exporters or persistent storage — all data lives in bounded memory structures with ring semantics.

Maps to proposal's approach: three-panel dashboard (logs, metrics, traces) fed by SignalR push, reusing existing auth patterns and SignalR infrastructure.

## Architecture Decisions

### Decision: Ring Buffer Location

| Option | Tradeoff | Decision |
|--------|----------|----------|
| **Infrastructure/Observability** | Centralized, follows existing adapter pattern, testable in isolation | ✅ **CHOSEN** |
| Separate project | Overkill for 3 buffers, adds project complexity | Rejected |
| API project | Violates Clean Architecture — Infrastructure should own telemetry adapters | Rejected |

**Rationale**: Existing pattern places adapters in `Aura.Infrastructure/Adapters/{Domain}/`. Telemetry is infrastructure concern (listening to .NET diagnostics). Keeps API layer thin (hub only streams, doesn't collect).

### Decision: Log Subscription Mechanism

| Option | Tradeoff | Decision |
|--------|----------|----------|
| **ILoggerProvider → IObservable<LogRecord>** | Standard .NET pattern, non-invasive, works with existing `[LoggerMessage]` | ✅ **CHOSEN** |
| Custom ILogEventSink | Serilog-specific, adds dependency | Rejected |
| ActivityListener duplicate | Conflates logs with traces, loses log metadata | Rejected |

**Rationale**: `ILoggerProvider` is the standard extensibility point. Returns `IObservable<LogRecord>` so buffer can subscribe without blocking hot paths. Existing `CorrelationMiddleware` already uses `ILogger.BeginScope` — logs already carry CorrelationId.

### Decision: SignalR Hub Topology

| Option | Tradeoff | Decision |
|--------|----------|----------|
| **Single TelemetryHub** | One connection, simpler client code, matches existing AlertHub pattern | ✅ **CHOSEN** |
| Per-signal hub (LogsHub, MetricsHub, TracesHub) | 3 connections per client, overkill for dashboard | Rejected |
| Multiplexed via groups | Adds complexity, no benefit for broadcast-only telemetry | Rejected |

**Rationale**: Existing `AlertHub` uses single-hub-per-domain pattern. Telemetry is broadcast-only (no client-specific filtering). One hub with three streaming methods (`StreamLogs`, `StreamMetrics`, `StreamTraces`) keeps client code simple.

### Decision: Streaming Model

| Option | Tradeoff | Decision |
|--------|----------|----------|
| **Timer-poll (1s interval) + push-on-change** | Balances latency vs. load; timer ensures liveness, push ensures immediacy | ✅ **CHOSEN** |
| Timer-poll only | 1s latency acceptable, but misses "instant" feel | Rejected |
| Channel<T> streaming | Overkill for broadcast, adds backpressure complexity | Rejected |
| Push-on-change only | No liveness guarantee if data stops flowing | Rejected |

**Rationale**: Spec requires "within 1 second" delivery. Timer-poll at 1s interval satisfies this. Push-on-change (via `IObservable` subscription) ensures new data appears immediately. Hybrid approach: timer fires every 1s, but if new data arrives, push immediately and reset timer.

### Decision: Blazor Rendering Strategy

| Option | Tradeoff | Decision |
|--------|----------|----------|
| **Virtual scroll (log table)** | Handles high-throughput without DOM bloat, matches spec requirement | ✅ **CHOSEN** |
| Real-time table (full DOM) | DOM bloat at 1000+ rows, poor performance | Rejected |
| Paginated | Breaks real-time feel, user must navigate pages | Rejected |

**Rationale**: Spec explicitly requires virtual scrolling for log panel. Metrics panel uses simple gauges (4 counters). Trace panel uses expandable rows (low volume, ~50 spans). Virtual scroll for logs via `<Virtualize>` component or custom implementation.

### Decision: Trace Collection Approach

| Option | Tradeoff | Decision |
|--------|----------|----------|
| **ActivityListener start/stop callbacks** | Captures full span lifecycle, duration, tags, status | ✅ **CHOSEN** |
| Polling Activity.Current | Misses short-lived spans, no duration, no tags | Rejected |

**Rationale**: `ActivityListener` provides `ActivityStarted` and `ActivityStopped` callbacks. Captures duration (stop - start), tags (via `activity.Tags`), status (via `activity.Status`). Existing codebase already uses `ActivitySource.StartActivity()` — listeners just observe.

### Decision: DI Registration Pattern

| Option | Tradeoff | Decision |
|--------|----------|----------|
| **Extension method AddObservability()** | Follows existing pattern (`AddAuraInfrastructure`), testable, discoverable | ✅ **CHOSEN** |
| Inline in Program.cs | Clutters startup, harder to test | Rejected |

**Rationale**: Existing pattern uses extension methods (`AddAuraInfrastructure`, `AddAuraApplication`). Keeps `Program.cs` clean. Extension method lives in Infrastructure project, registers buffers + listeners as singletons.

## Module Responsibilities

### Aura.Infrastructure/Observability

**Responsibility**: Collect telemetry from .NET diagnostics and store in ring buffers.

**Components**:
- `TelemetryBuffer<T>`: Generic bounded ring buffer (thread-safe, non-blocking producers)
- `LogRecordBuffer`: Specialized buffer for log records (capacity: 1000)
- `SpanBuffer`: Specialized buffer for activity spans (capacity: 500)
- `MetricSnapshotBuffer`: Specialized buffer for metric snapshots (capacity: 100)
- `TelemetryLoggerProvider`: `ILoggerProvider` that subscribes to log records
- `TelemetryActivityListener`: Wraps `ActivityListener` to capture spans
- `TelemetryMeterListener`: Wraps `MeterListener` to capture metric snapshots
- `ObservabilityExtensions`: DI registration extension method

### Aura.Api/Hubs

**Responsibility**: Stream telemetry to connected clients via SignalR.

**Components**:
- `TelemetryHub`: SignalR hub with three streaming methods
- `TelemetryStreamService`: Background service that polls buffers and pushes to hub

### Aura.UI/Pages

**Responsibility**: Render telemetry dashboard with real-time updates.

**Components**:
- `Observability.razor`: Three-panel page (logs, metrics, traces)
- `TelemetryClient`: SignalR client wrapper with auto-reconnect

### Aura.UI/Services

**Responsibility**: Manage SignalR connection and expose telemetry data to Blazor components.

**Components**:
- `TelemetryClient`: Wraps `HubConnection`, exposes `IObservable<T>` for each telemetry type
- `TelemetryState`: Singleton holding current buffer snapshots (for initial load)

## Detailed Design

### TelemetryBuffer<T>

```csharp
namespace Aura.Infrastructure.Observability;

/// <summary>
/// Thread-safe bounded ring buffer. Producers never block — oldest entry evicted when full.
/// </summary>
public sealed class TelemetryBuffer<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly int _capacity;
    private readonly object _trimLock = new();

    public TelemetryBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public void Write(T item)
    {
        _queue.Enqueue(item);
        if (_queue.Count > _capacity)
        {
            lock (_trimLock)
            {
                while (_queue.Count > _capacity && _queue.TryDequeue(out _)) { }
            }
        }
    }

    public IReadOnlyList<T> Snapshot()
    {
        return _queue.ToArray();
    }

    public int Count => _queue.Count;
}
```

### LogRecord DTO

```csharp
namespace Aura.Infrastructure.Observability;

public sealed record LogRecordDto(
    LogLevel Level,
    DateTimeOffset Timestamp,
    string CorrelationId,
    string Message,
    string Source);
```

### TelemetryLoggerProvider

```csharp
namespace Aura.Infrastructure.Observability;

/// <summary>
/// ILoggerProvider that captures log records into a ring buffer.
/// Subscribes to ILoggerFactory and forwards all log calls to the buffer.
/// </summary>
[ProviderAlias("Telemetry")]
public sealed class TelemetryLoggerProvider : ILoggerProvider
{
    private readonly LogRecordBuffer _buffer;

    public TelemetryLoggerProvider(LogRecordBuffer buffer)
    {
        _buffer = buffer;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TelemetryLogger(categoryName, _buffer);
    }

    public void Dispose() { }

    private sealed class TelemetryLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly LogRecordBuffer _buffer;

        public TelemetryLogger(string categoryName, LogRecordBuffer buffer)
        {
            _categoryName = categoryName;
            _buffer = buffer;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            var correlationId = ExtractCorrelationId(state);
            var record = new LogRecordDto(
                logLevel,
                DateTimeOffset.UtcNow,
                correlationId,
                message,
                _categoryName);

            _buffer.Write(record);
        }

        private static string ExtractCorrelationId<TState>(TState state)
        {
            // Extract from ILogger.BeginScope state if available
            if (state is IReadOnlyList<KeyValuePair<string, object?>> scope)
            {
                var kvp = scope.FirstOrDefault(k => k.Key == "CorrelationId");
                if (kvp.Value is string correlationId)
                    return correlationId;
            }
            return string.Empty;
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
```

### TelemetryActivityListener

```csharp
namespace Aura.Infrastructure.Observability;

/// <summary>
/// Captures Activity spans into a ring buffer using ActivityListener.
/// Subscribes to all ActivitySources in the process.
/// </summary>
public sealed class TelemetryActivityListener : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly SpanBuffer _buffer;

    public TelemetryActivityListener(SpanBuffer buffer)
    {
        _buffer = buffer;
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true, // Listen to all sources
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = OnActivityStarted,
            ActivityStopped = OnActivityStopped
        };
        ActivitySource.AddActivityListener(_listener);
    }

    private void OnActivityStarted(Activity activity)
    {
        // No-op — we capture on stop to get duration
    }

    private void OnActivityStopped(Activity activity)
    {
        var span = new SpanDto(
            activity.OperationName,
            activity.Duration.TotalMilliseconds,
            activity.StartTimeUtc,
            activity.Status == ActivityStatusCode.Ok ? "Healthy" : "Unhealthy",
            activity.Tags.ToDictionary(t => t.Key, t => t.Value));

        _buffer.Write(span);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}

public sealed record SpanDto(
    string OperationName,
    double DurationMs,
    DateTimeOffset StartTime,
    string Status,
    IReadOnlyDictionary<string, string?> Tags);
```

### TelemetryMeterListener

```csharp
namespace Aura.Infrastructure.Observability;

/// <summary>
/// Captures metric snapshots into a ring buffer using MeterListener.
/// Subscribes to all Meters and captures counter values on each measurement.
/// </summary>
public sealed class TelemetryMeterListener : IDisposable
{
    private readonly MeterListener _listener;
    private readonly MetricSnapshotBuffer _buffer;

    public TelemetryMeterListener(MetricSnapshotBuffer buffer)
    {
        _buffer = buffer;
        _listener = new MeterListener();
        _listener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
        _listener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _listener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            // Subscribe to all counters
            if (instrument is Counter<int> or Counter<long> or Counter<double>)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.Start();
    }

    private void OnMeasurementRecorded<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state) where T : struct
    {
        var snapshot = new MetricSnapshotDto(
            instrument.Name,
            Convert.ToDouble(measurement),
            DateTimeOffset.UtcNow,
            tags.ToDictionary(t => t.Key, t => t.Value?.ToString()));

        _buffer.Write(snapshot);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}

public sealed record MetricSnapshotDto(
    string Name,
    double Value,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string?> Tags);
```

### TelemetryHub

```csharp
namespace Aura.Api.Hubs;

/// <summary>
/// SignalR hub for streaming telemetry to connected clients.
/// Exposes three streaming methods: logs, metrics, traces.
/// </summary>
[Authorize]
public sealed class TelemetryHub : Hub
{
    private readonly LogRecordBuffer _logBuffer;
    private readonly SpanBuffer _spanBuffer;
    private readonly MetricSnapshotBuffer _metricBuffer;

    public TelemetryHub(
        LogRecordBuffer logBuffer,
        SpanBuffer spanBuffer,
        MetricSnapshotBuffer metricBuffer)
    {
        _logBuffer = logBuffer;
        _spanBuffer = spanBuffer;
        _metricBuffer = metricBuffer;
    }

    /// <summary>
    /// Streams log records to the client. Called by client on connect.
    /// </summary>
    public async IAsyncEnumerable<IReadOnlyList<LogRecordDto>> StreamLogs(
        [EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return _logBuffer.Snapshot();
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }

    /// <summary>
    /// Streams metric snapshots to the client.
    /// </summary>
    public async IAsyncEnumerable<IReadOnlyList<MetricSnapshotDto>> StreamMetrics(
        [EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return _metricBuffer.Snapshot();
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }

    /// <summary>
    /// Streams spans to the client.
    /// </summary>
    public async IAsyncEnumerable<IReadOnlyList<SpanDto>> StreamTraces(
        [EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return _spanBuffer.Snapshot();
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }
}
```

### TelemetryStreamService (Background Service)

```csharp
namespace Aura.Api.Services;

/// <summary>
/// Background service that polls telemetry buffers and pushes updates to connected SignalR clients.
/// Complements the hub's streaming methods by ensuring liveness even if client doesn't call Stream*.
/// </summary>
public sealed class TelemetryStreamService : BackgroundService
{
    private readonly IHubContext<TelemetryHub> _hubContext;
    private readonly LogRecordBuffer _logBuffer;
    private readonly SpanBuffer _spanBuffer;
    private readonly MetricSnapshotBuffer _metricBuffer;
    private readonly ILogger<TelemetryStreamService> _logger;

    public TelemetryStreamService(
        IHubContext<TelemetryHub> hubContext,
        LogRecordBuffer logBuffer,
        SpanBuffer spanBuffer,
        MetricSnapshotBuffer metricBuffer,
        ILogger<TelemetryStreamService> logger)
    {
        _hubContext = hubContext;
        _logBuffer = logBuffer;
        _spanBuffer = spanBuffer;
        _metricBuffer = metricBuffer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _hubContext.Clients.All.SendAsync(
                    "ReceiveLogs",
                    _logBuffer.Snapshot(),
                    stoppingToken);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveMetrics",
                    _metricBuffer.Snapshot(),
                    stoppingToken);

                await _hubContext.Clients.All.SendAsync(
                    "ReceiveTraces",
                    _spanBuffer.Snapshot(),
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to push telemetry to SignalR clients");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
```

### ObservabilityExtensions (DI Registration)

```csharp
namespace Aura.Infrastructure;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Registers telemetry buffers and listeners for the observability dashboard.
    /// </summary>
    public static IServiceCollection AddAuraObservability(this IServiceCollection services)
    {
        // Buffers (singletons — shared across all consumers)
        services.AddSingleton<LogRecordBuffer>(new LogRecordBuffer(capacity: 1000));
        services.AddSingleton<SpanBuffer>(new SpanBuffer(capacity: 500));
        services.AddSingleton<MetricSnapshotBuffer>(new MetricSnapshotBuffer(capacity: 100));

        // Listeners (singletons — subscribe to .NET diagnostics)
        services.AddSingleton<TelemetryActivityListener>(sp =>
            new TelemetryActivityListener(sp.GetRequiredService<SpanBuffer>()));

        services.AddSingleton<TelemetryMeterListener>(sp =>
            new TelemetryMeterListener(sp.GetRequiredService<MetricSnapshotBuffer>()));

        // Logger provider (subscribes to ILoggerFactory)
        services.AddSingleton<ILoggerProvider, TelemetryLoggerProvider>(sp =>
            new TelemetryLoggerProvider(sp.GetRequiredService<LogRecordBuffer>()));

        return services;
    }
}
```

### TelemetryClient (UI SignalR Client)

```csharp
namespace Aura.UI.Services;

/// <summary>
/// SignalR client wrapper for telemetry streaming.
/// Auto-reconnects with exponential backoff and re-subscribes on reconnect.
/// </summary>
public sealed class TelemetryClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<TelemetryClient> _logger;
    private readonly CancellationTokenSource _cts = new();

    public event Action<IReadOnlyList<LogRecordDto>>? LogsReceived;
    public event Action<IReadOnlyList<MetricSnapshotDto>>? MetricsReceived;
    public event Action<IReadOnlyList<SpanDto>>? TracesReceived;

    public TelemetryClient(IConfiguration config, ILogger<TelemetryClient> logger)
    {
        _logger = logger;
        var baseUrl = config["AuraApi:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
        var hubUrl = string.IsNullOrEmpty(baseUrl)
            ? "/hubs/telemetry"
            : $"{baseUrl}/hubs/telemetry";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                // Token acquisition handled by ForwardedAccessTokenHandler pattern
            })
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5) })
            .Build();

        _connection.On<IReadOnlyList<LogRecordDto>>("ReceiveLogs", logs =>
        {
            LogsReceived?.Invoke(logs);
        });

        _connection.On<IReadOnlyList<MetricSnapshotDto>>("ReceiveMetrics", metrics =>
        {
            MetricsReceived?.Invoke(metrics);
        });

        _connection.On<IReadOnlyList<SpanDto>>("ReceiveTraces", traces =>
        {
            TracesReceived?.Invoke(traces);
        });
    }

    public async Task StartAsync()
    {
        await _connection.StartAsync(_cts.Token);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await _connection.DisposeAsync();
    }
}
```

### Observability.razor (Blazor Page)

```razor
@page "/observability"
@using Aura.UI.Services
@using Aura.UI.Components.Auth
@using Microsoft.AspNetCore.Components.Authorization
@inject TelemetryClient TelemetryClient
@implements IAsyncDisposable

<AuthorizeView>
    <Authorized>
        <PageTitle>Aura - Observability</PageTitle>

        <div class="dashboard-page-header">
            <h1 class="dashboard-page-header__title">Observability Dashboard</h1>
            <p class="dashboard-page-header__subtitle">Real-time telemetry: logs, metrics, and traces</p>
        </div>

        <div class="observability-grid">
            <!-- Log Panel -->
            <section class="dashboard-panel">
                <h2>Logs</h2>
                <div class="log-table-container" style="max-height: 400px; overflow-y: auto;">
                    <table class="decision-log-table">
                        <thead>
                            <tr>
                                <th>Level</th>
                                <th>Timestamp</th>
                                <th>CorrelationId</th>
                                <th>Message</th>
                                <th>Source</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var log in _logs)
                            {
                                <tr class="log-row log-row--@log.Level.ToString().ToLower()">
                                    <td>@log.Level</td>
                                    <td>@log.Timestamp.ToLocalTime().ToString("HH:mm:ss.fff")</td>
                                    <td style="font-family: var(--aura-font-code); font-size: 11px;">@log.CorrelationId</td>
                                    <td>@log.Message</td>
                                    <td>@log.Source</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            </section>

            <!-- Metrics Panel -->
            <section class="dashboard-panel">
                <h2>Metrics</h2>
                <div class="metrics-grid">
                    @foreach (var metric in _metrics)
                    {
                        <div class="metric-gauge">
                            <div class="metric-gauge__name">@metric.Name</div>
                            <div class="metric-gauge__value">@metric.Value.ToString("N0")</div>
                        </div>
                    }
                </div>
            </section>

            <!-- Trace Panel -->
            <section class="dashboard-panel">
                <h2>Traces</h2>
                <table class="decision-log-table">
                    <thead>
                        <tr>
                            <th>Operation</th>
                            <th>Duration (ms)</th>
                            <th>Start Time</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var span in _traces)
                        {
                            <tr>
                                <td>@span.OperationName</td>
                                <td>@span.DurationMs.ToString("N2")</td>
                                <td>@span.StartTime.ToLocalTime().ToString("HH:mm:ss.fff")</td>
                                <td>
                                    <span class="status-badge status-badge--@span.Status.ToLower()">
                                        @span.Status
                                    </span>
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            </section>
        </div>
    </Authorized>
    <NotAuthorized>
        <PageTitle>Aura | Access Required</PageTitle>
        <RedirectToLanding />
    </NotAuthorized>
</AuthorizeView>

@code {
    private IReadOnlyList<LogRecordDto> _logs = Array.Empty<LogRecordDto>();
    private IReadOnlyList<MetricSnapshotDto> _metrics = Array.Empty<MetricSnapshotDto>();
    private IReadOnlyList<SpanDto> _traces = Array.Empty<SpanDto>();

    protected override async Task OnInitializedAsync()
    {
        TelemetryClient.LogsReceived += OnLogsReceived;
        TelemetryClient.MetricsReceived += OnMetricsReceived;
        TelemetryClient.TracesReceived += OnTracesReceived;

        await TelemetryClient.StartAsync();
    }

    private void OnLogsReceived(IReadOnlyList<LogRecordDto> logs)
    {
        _logs = logs;
        InvokeAsync(StateHasChanged);
    }

    private void OnMetricsReceived(IReadOnlyList<MetricSnapshotDto> metrics)
    {
        _metrics = metrics;
        InvokeAsync(StateHasChanged);
    }

    private void OnTracesReceived(IReadOnlyList<SpanDto> traces)
    {
        _traces = traces;
        InvokeAsync(StateHasChanged);
    }

    public async ValueTask DisposeAsync()
    {
        TelemetryClient.LogsReceived -= OnLogsReceived;
        TelemetryClient.MetricsReceived -= OnMetricsReceived;
        TelemetryClient.TracesReceived -= OnTracesReceived;

        await TelemetryClient.DisposeAsync();
    }
}
```

## Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                        .NET Diagnostics                          │
│  ILogger ──────┐                                                │
│  ActivitySource├──────► Listeners (Infrastructure)              │
│  Meter         ┘         │                                       │
│                          ▼                                       │
│                    Ring Buffers                                  │
│                    - LogRecordBuffer (1000)                      │
│                    - SpanBuffer (500)                            │
│                    - MetricSnapshotBuffer (100)                  │
│                          │                                       │
└──────────────────────────┼───────────────────────────────────────┘
                           │
                           ▼
              ┌────────────────────────┐
              │  TelemetryStreamService│
              │  (Background Service)  │
              │  Polls every 1s        │
              └────────┬───────────────┘
                       │
                       ▼
              ┌────────────────────────┐
              │    TelemetryHub        │
              │    (SignalR)           │
              │  /hubs/telemetry       │
              └────────┬───────────────┘
                       │
                       │ WebSocket
                       ▼
              ┌────────────────────────┐
              │   TelemetryClient      │
              │   (UI SignalR Client)  │
              └────────┬───────────────┘
                       │
                       │ Events
                       ▼
              ┌────────────────────────┐
              │  Observability.razor   │
              │  (Blazor Page)         │
              │  - Log Panel           │
              │  - Metrics Panel       │
              │  - Trace Panel         │
              └────────────────────────┘
```

## Sequence Diagram: Log Streaming

```
User                Blazor Page         TelemetryClient       TelemetryHub        StreamService       LogBuffer
 │                      │                      │                    │                    │                  │
 │ Navigate to          │                      │                    │                    │                  │
 │ /observability       │                      │                    │                    │                  │
 ├─────────────────────►│                      │                    │                    │                  │
 │                      │ StartAsync()         │                    │                    │                  │
 │                      ├─────────────────────►│                    │                    │                  │
 │                      │                      │ Connect to         │                    │                  │
 │                      │                      │ /hubs/telemetry    │                    │                  │
 │                      │                      ├───────────────────►│                    │                  │
 │                      │                      │                    │                    │                  │
 │                      │                      │                    │   Poll every 1s    │                  │
 │                      │                      │                    │◄───────────────────┤                  │
 │                      │                      │                    │                    │ Snapshot()       │
 │                      │                      │                    │                    ├─────────────────►│
 │                      │                      │                    │                    │                  │
 │                      │                      │ ReceiveLogs(batch) │                    │                  │
 │                      │                      │◄───────────────────┤                    │                  │
 │                      │ LogsReceived         │                    │                    │                  │
 │                      │◄─────────────────────┤                    │                    │                  │
 │                      │ StateHasChanged()    │                    │                    │                  │
 │                      │ Render log table     │                    │                    │                  │
 │                      │                      │                    │                    │                  │
```

## Interfaces / Contracts

### Buffer Contracts

```csharp
// Generic buffer interface (optional — concrete classes used for DI)
public interface ITelemetryBuffer<T>
{
    void Write(T item);
    IReadOnlyList<T> Snapshot();
    int Count { get;
}

// Specialized buffers (type aliases for clarity)
public sealed class LogRecordBuffer : TelemetryBuffer<LogRecordDto>
{
    public LogRecordBuffer(int capacity) : base(capacity) { }
}

public sealed class SpanBuffer : TelemetryBuffer<SpanDto>
{
    public SpanBuffer(int capacity) : base(capacity) { }
}

public sealed class MetricSnapshotBuffer : TelemetryBuffer<MetricSnapshotDto>
{
    public MetricSnapshotBuffer(int capacity) : base(capacity) { }
}
```

### SignalR Hub Contract

```csharp
// Client → Server
public interface ITelemetryHubClient
{
    Task ReceiveLogs(IReadOnlyList<LogRecordDto> logs);
    Task ReceiveMetrics(IReadOnlyList<MetricSnapshotDto> metrics);
    Task ReceiveTraces(IReadOnlyList<SpanDto> traces);
}

// Server → Client (streaming methods)
// IAsyncEnumerable<T> StreamLogs(CancellationToken ct)
// IAsyncEnumerable<T> StreamMetrics(CancellationToken ct)
// IAsyncEnumerable<T> StreamTraces(CancellationToken ct)
```

## Configuration

No new configuration required. Existing settings reused:
- `AuraApi:BaseUrl` — API base URL for SignalR client
- Auth — existing cookie/OIDC pattern applies to `/observability` route

Optional future configuration (not in v1):
```json
{
  "Observability": {
    "LogBufferCapacity": 1000,
    "SpanBufferCapacity": 500,
    "MetricBufferCapacity": 100,
    "StreamIntervalSeconds": 1
  }
}
```

## Dependencies

### NuGet Packages
**None** — all APIs exist in .NET 9 / ASP.NET Core:
- `System.Diagnostics.ActivityListener` (built-in)
- `System.Diagnostics.Metrics.MeterListener` (built-in)
- `Microsoft.AspNetCore.SignalR` (built-in)
- `Microsoft.Extensions.Logging` (built-in)

### Internal Dependencies
- `Aura.Infrastructure` → `Aura.Application` (for ports, if any)
- `Aura.Api` → `Aura.Infrastructure` (for buffers)
- `Aura.UI` → `Aura.Infrastructure` (for DTOs)

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/Aura.Infrastructure/Observability/TelemetryBuffer.cs` | Create | Generic ring buffer |
| `src/Aura.Infrastructure/Observability/LogRecordBuffer.cs` | Create | Specialized log buffer |
| `src/Aura.Infrastructure/Observability/SpanBuffer.cs` | Create | Specialized span buffer |
| `src/Aura.Infrastructure/Observability/MetricSnapshotBuffer.cs` | Create | Specialized metric buffer |
| `src/Aura.Infrastructure/Observability/TelemetryLoggerProvider.cs` | Create | ILoggerProvider for log capture |
| `src/Aura.Infrastructure/Observability/TelemetryActivityListener.cs` | Create | ActivityListener wrapper |
| `src/Aura.Infrastructure/Observability/TelemetryMeterListener.cs` | Create | MeterListener wrapper |
| `src/Aura.Infrastructure/Observability/ObservabilityExtensions.cs` | Create | DI registration |
| `src/Aura.Infrastructure/Observability/Dtos.cs` | Create | LogRecordDto, SpanDto, MetricSnapshotDto |
| `src/Aura.Api/Hubs/TelemetryHub.cs` | Create | SignalR hub |
| `src/Aura.Api/Services/TelemetryStreamService.cs` | Create | Background service for polling |
| `src/Aura.Api/Program.cs` | Modify | Register observability + map hub + add background service |
| `src/Aura.UI/Pages/Observability.razor` | Create | Three-panel dashboard page |
| `src/Aura.UI/Services/TelemetryClient.cs` | Create | SignalR client wrapper |
| `src/Aura.UI/Program.cs` | Modify | Register TelemetryClient |

**Total**: 13 new files, 2 modified files

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| **Unit** | `TelemetryBuffer<T>` ring semantics (eviction, thread-safety) | xUnit + parallel writes test |
| **Unit** | `TelemetryLoggerProvider` log capture | Mock ILogger, verify buffer receives records |
| **Unit** | `TelemetryActivityListener` span capture | Create Activity, verify buffer receives spans |
| **Unit** | `TelemetryMeterListener` metric capture | Increment counter, verify buffer receives snapshots |
| **Integration** | `TelemetryHub` streaming | Use `HubConnection` test client, verify data flows |
| **Integration** | End-to-end log flow | Emit log → verify appears in buffer → verify hub streams it |
| **E2E** | Blazor page rendering | Use `bUnit` to render `Observability.razor`, verify panels populate |

### Key Test Scenarios
1. **Buffer eviction**: Write 1500 logs to 1000-capacity buffer → verify oldest 500 evicted
2. **Concurrent producers**: 10 threads write 100 logs each → verify no blocking, buffer contains last 1000
3. **Log capture**: Emit log with CorrelationId → verify buffer contains record with correct fields
4. **Span capture**: Start/stop Activity with tags → verify buffer contains span with duration and tags
5. **Metric capture**: Increment counter by 3 → verify buffer contains snapshot with value 3
6. **SignalR streaming**: Connect client → verify receives data within 1 second

## Migration / Rollout

**No migration required.** This is a greenfield feature with no schema changes, no data migration, no feature flags.

**Rollout**:
1. Deploy API with new observability endpoints and hub
2. Deploy UI with new `/observability` page
3. Feature is immediately available to authenticated users

**Rollback**: Revert 2 files (API `Program.cs`, UI `Program.cs`), delete 13 new files. No config changes, no database, no schema.

## Open Questions

- [ ] **Virtual scroll implementation**: Should we use `<Virtualize>` component (built-in) or custom implementation? `<Virtualize>` is simpler but may not handle real-time updates well. Custom implementation gives more control.
- [ ] **Push-on-change optimization**: Should we implement push-on-change (via `IObservable` subscription in hub) or rely solely on timer-poll? Push-on-change reduces latency but adds complexity.
- [ ] **Metric aggregation**: Should we show latest counter value or aggregate (sum/avg) over time? Spec says "latest snapshot value" but counters are cumulative — may need delta calculation.
- [ ] **Log filtering**: Should we add client-side filtering (by level, source, correlationId) in v1 or defer to v2?
- [ ] **Trace expansion**: Spec mentions "expanding a span reveals tags" — should we use collapsible rows or a modal? Collapsible rows are simpler but may clutter the table.

## Next Steps

Ready for tasks (sdd-tasks). Break down into implementation tasks:
1. Create ring buffer infrastructure (TelemetryBuffer + specialized buffers)
2. Implement listeners (LoggerProvider, ActivityListener, MeterListener)
3. Create TelemetryHub + TelemetryStreamService
4. Build Blazor page + TelemetryClient
5. Wire up DI registration
6. Write unit + integration tests
7. E2E test with bUnit
