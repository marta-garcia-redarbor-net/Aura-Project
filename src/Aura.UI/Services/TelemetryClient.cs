using Aura.Infrastructure.Observability;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.UI.Services;

/// <summary>
/// SignalR client wrapper for telemetry streaming.
/// Uses the same token acquisition pattern as other SignalR listeners.
/// Auto-reconnects with backoff and re-subscribes on reconnect.
/// </summary>
public sealed class TelemetryClient : IAsyncDisposable
{
    private readonly ITokenAcquisitionService _tokenService;
    private readonly IConfiguration _config;
    private readonly ILogger<TelemetryClient> _logger;
    private readonly CancellationTokenSource _cts = new();
    private HubConnection? _connection;

    public event Action<IReadOnlyList<LogRecordDto>>? LogsReceived;
    public event Action<IReadOnlyList<MetricSnapshotDto>>? MetricsReceived;
    public event Action<IReadOnlyList<SpanDto>>? TracesReceived;

    public TelemetryClient(
        ITokenAcquisitionService tokenService,
        IConfiguration config,
        ILogger<TelemetryClient> logger)
    {
        _tokenService = tokenService;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        try
        {
            await ConnectWithRetryAsync(maxRetries: 3, delayMs: 1000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start TelemetryClient connection after retries");
        }
    }

    private async Task ConnectWithRetryAsync(int maxRetries, int delayMs)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var token = await _tokenService.AcquireTokenAsync(_cts.Token);

                var baseUrl = _config["AuraApi:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
                var hubUrl = string.IsNullOrEmpty(baseUrl)
                    ? "/hubs/telemetry"
                    : $"{baseUrl}/hubs/telemetry";

                _connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(token)!;
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

                await _connection.StartAsync(_cts.Token);
                _logger.LogInformation("TelemetryClient connected to {HubUrl}", hubUrl);
                return; // success
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex,
                    "TelemetryClient connection attempt {Attempt}/{MaxRetries} failed",
                    attempt, maxRetries);
                await Task.Delay(delayMs, _cts.Token);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
        _cts.Dispose();
    }
}
