using Aura.Application.UseCases.Calendar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Workers;

/// <summary>
/// Background service that polls for upcoming meeting alerts and dispatches them via SignalR.
/// Moved from Aura.Workers to Api to co-locate with the SignalR hub.
/// </summary>
public sealed partial class MeetingAlertWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MeetingAlertWorker> _logger;

    public MeetingAlertWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<MeetingAlertWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var useCase = scope.ServiceProvider.GetRequiredService<CheckAndDispatchMeetingAlertsUseCase>();
                    await useCase.ExecuteAsync(DateTimeOffset.UtcNow, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.PollFailed(_logger, ex);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            Log.WorkerCancelled(_logger);
        }
        catch (Exception ex)
        {
            Log.WorkerCrashed(_logger, ex);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 4401, Level = LogLevel.Warning,
            Message = "MeetingAlertWorker poll failed")]
        public static partial void PollFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 4402, Level = LogLevel.Information,
            Message = "MeetingAlertWorker cancelled")]
        public static partial void WorkerCancelled(ILogger logger);

        [LoggerMessage(EventId = 4403, Level = LogLevel.Error,
            Message = "MeetingAlertWorker crashed unexpectedly")]
        public static partial void WorkerCrashed(ILogger logger, Exception exception);
    }
}
