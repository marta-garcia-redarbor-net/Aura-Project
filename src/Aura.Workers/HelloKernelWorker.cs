using Aura.Application.Kernel;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging;

namespace Aura.Workers;

/// <summary>
/// Fire-once background worker that validates the kernel pipeline wiring.
/// Creates a dummy <see cref="WorkItem"/>, executes the plugin pipeline, logs the result, and stops.
/// </summary>
public sealed partial class HelloKernelWorker : BackgroundService
{
    private readonly IPluginRegistry _registry;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<HelloKernelWorker> _logger;

    public HelloKernelWorker(
        IPluginRegistry registry,
        IHostApplicationLifetime lifetime,
        ILogger<HelloKernelWorker> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.WorkerStarted(_logger);

        var correlationId = Guid.NewGuid().ToString();
        var workItem = new WorkItem(
            externalId: $"hello-worker-{Guid.NewGuid():N}",
            title: "Hello Kernel Validation",
            source: "hello-worker",
            sourceType: WorkItemSourceType.TodoTask,
            priority: WorkItemPriority.Low,
            metadata: new Dictionary<string, string>
            {
                ["worker"] = nameof(HelloKernelWorker),
                ["scenario"] = "kernel-validation"
            },
            correlationId: correlationId,
            capturedAtUtc: null);

        try
        {
            await _registry.ExecuteAsync(workItem, stoppingToken);

            Log.WorkerCompleted(_logger,
                workItem.Id,
                workItem.Status,
                workItem.ExternalId,
                workItem.SourceType,
                workItem.Priority,
                workItem.CorrelationId);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            Log.WorkerCancelled(_logger);
        }
        catch (Exception ex)
        {
            Log.WorkerFailed(_logger, ex);
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 3201, Level = LogLevel.Information,
            Message = "HelloKernelWorker started — executing kernel pipeline validation")]
        public static partial void WorkerStarted(ILogger logger);

        [LoggerMessage(EventId = 3202, Level = LogLevel.Information,
            Message = "HelloKernelWorker completed. WorkItem {WorkItemId} final status: {Status}. ExternalId: {ExternalId}. SourceType: {SourceType}. Priority: {Priority}. CorrelationId: {CorrelationId}")]
        public static partial void WorkerCompleted(ILogger logger, Guid workItemId, WorkItemStatus status, string externalId, WorkItemSourceType sourceType, WorkItemPriority priority, string correlationId);

        [LoggerMessage(EventId = 3203, Level = LogLevel.Warning,
            Message = "HelloKernelWorker cancelled before completion")]
        public static partial void WorkerCancelled(ILogger logger);

        [LoggerMessage(EventId = 3204, Level = LogLevel.Error,
            Message = "HelloKernelWorker failed unexpectedly")]
        public static partial void WorkerFailed(ILogger logger, Exception exception);
    }
}
