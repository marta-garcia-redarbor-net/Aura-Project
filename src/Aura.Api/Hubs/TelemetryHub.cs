using Aura.Infrastructure.Observability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

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
        ArgumentNullException.ThrowIfNull(logBuffer);
        ArgumentNullException.ThrowIfNull(spanBuffer);
        ArgumentNullException.ThrowIfNull(metricBuffer);

        _logBuffer = logBuffer;
        _spanBuffer = spanBuffer;
        _metricBuffer = metricBuffer;
    }

    /// <summary>
    /// Streams log records to the client. Called by client on connect.
    /// </summary>
    public async IAsyncEnumerable<IReadOnlyList<LogRecordDto>> StreamLogs(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
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
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
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
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return _spanBuffer.Snapshot();
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }
    }
}
