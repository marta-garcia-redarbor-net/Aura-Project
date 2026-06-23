using Aura.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Aura.Workers;

public sealed partial class MorningSummarySchedulingWorker : BackgroundService
{
    private const string SystemUserId = "system";
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(1);

    private readonly IMorningSummaryScheduler _scheduler;
    private readonly IMorningSummaryEmissionStore _emissionStore;
    private readonly ILogger<MorningSummarySchedulingWorker> _logger;

    public MorningSummarySchedulingWorker(
        IMorningSummaryScheduler scheduler,
        IMorningSummaryEmissionStore emissionStore,
        ILogger<MorningSummarySchedulingWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(emissionStore);
        ArgumentNullException.ThrowIfNull(logger);

        _scheduler = scheduler;
        _emissionStore = emissionStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.WorkerStarted(_logger, SystemUserId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessIterationAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.WorkerFailed(_logger, ex);
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        Log.WorkerStopped(_logger);
    }

    internal async Task ProcessIterationAsync(CancellationToken ct)
    {
        var due = await _scheduler.ResolveAsync(SystemUserId, ct);

        if (!due.IsDue)
        {
            Log.NotDue(_logger, SystemUserId, due.LocalDate, due.TargetLocalTime, due.ResolvedTimezoneId);
            return;
        }

        await _emissionStore.MarkEmittedAsync(SystemUserId, due.LocalDate, ct);
        Log.MarkedEmitted(_logger, SystemUserId, due.LocalDate, due.ResolvedTimezoneId);
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 4301,
            Level = LogLevel.Information,
            Message = "MorningSummarySchedulingWorker started for fixed user {UserId}")]
        public static partial void WorkerStarted(ILogger logger, string userId);

        [LoggerMessage(
            EventId = 4302,
            Level = LogLevel.Information,
            Message = "Morning Summary is not due for {UserId}. LocalDate={LocalDate}, TargetLocalTime={TargetLocalTime}, Timezone={TimezoneId}")]
        public static partial void NotDue(ILogger logger, string userId, DateOnly localDate, TimeOnly targetLocalTime, string timezoneId);

        [LoggerMessage(
            EventId = 4303,
            Level = LogLevel.Information,
            Message = "Morning Summary emission marked for {UserId} on {LocalDate} ({TimezoneId})")]
        public static partial void MarkedEmitted(ILogger logger, string userId, DateOnly localDate, string timezoneId);

        [LoggerMessage(
            EventId = 4304,
            Level = LogLevel.Error,
            Message = "MorningSummarySchedulingWorker iteration failed")]
        public static partial void WorkerFailed(ILogger logger, Exception exception);

        [LoggerMessage(
            EventId = 4305,
            Level = LogLevel.Information,
            Message = "MorningSummarySchedulingWorker stopped")]
        public static partial void WorkerStopped(ILogger logger);
    }
}
