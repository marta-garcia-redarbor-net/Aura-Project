using Aura.Api.Hubs;
using Aura.Infrastructure.Observability;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        ILogger<TelemetryStreamService> logger,
        // Force instantiation of listeners so they start capturing telemetry.
        // They are not directly used here, but must be activated for their constructors to register
        // with the .NET diagnostics system (ActivityListener, MeterListener).
        TelemetryActivityListener activityListener,
        TelemetryMeterListener meterListener)
    {
        ArgumentNullException.ThrowIfNull(hubContext);
        ArgumentNullException.ThrowIfNull(logBuffer);
        ArgumentNullException.ThrowIfNull(spanBuffer);
        ArgumentNullException.ThrowIfNull(metricBuffer);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(activityListener);
        ArgumentNullException.ThrowIfNull(meterListener);

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
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to push telemetry to SignalR clients");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
