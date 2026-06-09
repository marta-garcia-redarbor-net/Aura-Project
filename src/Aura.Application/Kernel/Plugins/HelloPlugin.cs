using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging;

namespace Aura.Application.Kernel.Plugins;

/// <summary>
/// No-op plugin that logs execution to prove pipeline wiring.
/// </summary>
public sealed class HelloPlugin : IPlugin
{
    private readonly ILogger<HelloPlugin> _logger;

    public HelloPlugin(ILogger<HelloPlugin> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task ExecuteAsync(WorkItem item, CancellationToken ct)
    {
        _logger.LogInformation("HelloPlugin executed for WorkItem {WorkItemId} ({Title})",
            item.Id, item.Title);
        return Task.CompletedTask;
    }
}
