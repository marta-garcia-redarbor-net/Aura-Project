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

        var workItem = new WorkItem("Hello Kernel Validation", "hello-worker");

        try
        {
            await _registry.ExecuteAsync(workItem, stoppingToken);

            _logger.LogInformation(
                "HelloKernelWorker completed. WorkItem {WorkItemId} final status: {Status}",
                workItem.Id, workItem.Status);
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
