using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aura.Workers;

public sealed partial class MorningSummarySchedulingWorker : BackgroundService
{
    private const string SystemUserId = "system";
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MorningSummarySchedulingWorker> _logger;

    public MorningSummarySchedulingWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<MorningSummarySchedulingWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
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
        using var scope = _scopeFactory.CreateScope();

        var scheduler = scope.ServiceProvider.GetRequiredService<IMorningSummaryScheduler>();
        var emissionStore = scope.ServiceProvider.GetRequiredService<IMorningSummaryEmissionStore>();
        var composer = scope.ServiceProvider.GetRequiredService<IMorningSummaryComposer>();

        var due = await scheduler.ResolveAsync(SystemUserId, ct);

        if (!due.IsDue)
        {
            Log.NotDue(_logger, SystemUserId, due.LocalDate, due.TargetLocalTime, due.ResolvedTimezoneId);
            return;
        }

        await emissionStore.MarkEmittedAsync(SystemUserId, due.LocalDate, ct);
        Log.MarkedEmitted(_logger, SystemUserId, due.LocalDate, due.ResolvedTimezoneId);

        try
        {
            var window = new MorningSummaryWindow(
                due.LocalDate,
                due.ResolvedTimezoneId,
                due.TargetLocalTime,
                DateTimeOffset.UtcNow);

            var request = new MorningSummaryRequest(SystemUserId, window);
            await composer.ComposeAsync(request, ct);
            Log.CompositionSucceeded(_logger, SystemUserId, due.LocalDate);
        }
        catch (Exception ex)
        {
            Log.CompositionFailed(_logger, ex);
        }
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

        [LoggerMessage(
            EventId = 4306,
            Level = LogLevel.Information,
            Message = "Morning Summary composition succeeded for {UserId} on {LocalDate}")]
        public static partial void CompositionSucceeded(ILogger logger, string userId, DateOnly localDate);

        [LoggerMessage(
            EventId = 4307,
            Level = LogLevel.Error,
            Message = "Morning Summary composition failed")]
        public static partial void CompositionFailed(ILogger logger, Exception exception);
    }
}
