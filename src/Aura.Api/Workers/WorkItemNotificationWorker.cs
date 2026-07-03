using Aura.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Workers;

/// <summary>
/// Background service that polls the <see cref="INotificationOutboxStore"/> for pending entries
/// and dispatches them via <see cref="IWorkItemNotificationDispatcher"/>.
/// Polls every 2 seconds for low-latency delivery.
/// </summary>
public sealed partial class WorkItemNotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WorkItemNotificationWorker> _logger;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    public WorkItemNotificationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<WorkItemNotificationWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.WorkerStarting(_logger);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var store = scope.ServiceProvider.GetRequiredService<INotificationOutboxStore>();
                    var dispatcher = scope.ServiceProvider.GetRequiredService<IWorkItemNotificationDispatcher>();

                    var pending = await store.GetPendingAsync(10, stoppingToken);

                    foreach (var entry in pending)
                    {
                        // Use a simple verdict for dispatch - the verdict is already baked into
                        // the entry's TriggerRule and Priority from when it was enqueued.
                        var verdict = new Aura.Application.Models.InterruptionVerdict(
                            Aura.Application.Models.InterruptionDecision.InterruptNow,
                            new Aura.Application.Models.EvaluationReport(Array.Empty<Aura.Application.Models.RuleResult>()),
                            entry.TriggerRule);

                        await dispatcher.DispatchAsync(entry, verdict, stoppingToken);
                        await store.MarkDispatchedAsync(entry.Id, stoppingToken);

                        Log.EntryDispatched(_logger, entry.Id, entry.Title);
                    }
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
                    await Task.Delay(PollInterval, stoppingToken);
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
        [LoggerMessage(EventId = 4601, Level = LogLevel.Information,
            Message = "WorkItemNotificationWorker started")]
        public static partial void WorkerStarting(ILogger logger);

        [LoggerMessage(EventId = 4602, Level = LogLevel.Warning,
            Message = "WorkItemNotificationWorker poll failed")]
        public static partial void PollFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 4603, Level = LogLevel.Information,
            Message = "WorkItemNotificationWorker cancelled")]
        public static partial void WorkerCancelled(ILogger logger);

        [LoggerMessage(EventId = 4604, Level = LogLevel.Error,
            Message = "WorkItemNotificationWorker crashed unexpectedly")]
        public static partial void WorkerCrashed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 4605, Level = LogLevel.Information,
            Message = "WorkItem notification dispatched and marked: Id={NotificationId}, Title={Title}")]
        public static partial void EntryDispatched(ILogger logger, Guid notificationId, string title);
    }
}
