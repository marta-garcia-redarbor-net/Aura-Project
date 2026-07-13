using Aura.Infrastructure.Observability;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
        try
        {
            await _connection.StartAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start TelemetryClient connection");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        await _connection.DisposeAsync();
        _cts.Dispose();
    }
}
