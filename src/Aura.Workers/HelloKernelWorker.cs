using Aura.Application.Kernel;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging;

namespace Aura.Workers;

/// <summary>
/// Fire-once background worker that validates the kernel pipeline wiring.
/// Creates a dummy <see cref="WorkItem"/>, executes the plugin pipeline, logs the result, and stops.
/// </summary>
public sealed class HelloKernelWorker : BackgroundService
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
        _logger.LogInformation("HelloKernelWorker started — executing kernel pipeline validation");

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

            _logger.LogInformation(
                "HelloKernelWorker completed. WorkItem {WorkItemId} final status: {Status}. ExternalId: {ExternalId}. SourceType: {SourceType}. Priority: {Priority}. CorrelationId: {CorrelationId}",
                workItem.Id,
                workItem.Status,
                workItem.ExternalId,
                workItem.SourceType,
                workItem.Priority,
                workItem.CorrelationId);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning("HelloKernelWorker cancelled before completion");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HelloKernelWorker failed unexpectedly");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
