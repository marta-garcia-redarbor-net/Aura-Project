using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging;

namespace Aura.Application.Kernel;

/// <summary>
/// Sequential plugin execution pipeline.
/// Processes all registered plugins in order. On failure, marks the work item as faulted
/// and aborts remaining plugins, preserving worker process stability.
/// </summary>
public sealed class PluginRegistry : IPluginRegistry
{
    private readonly IReadOnlyList<IPlugin> _plugins;
    private readonly ILogger<PluginRegistry> _logger;

    public PluginRegistry(IEnumerable<IPlugin> plugins, ILogger<PluginRegistry> logger)
    {
        _plugins = (plugins ?? throw new ArgumentNullException(nameof(plugins))).ToArray();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(WorkItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (_plugins.Count == 0)
        {
            return;
        }

        item.MarkProcessing();

        foreach (var plugin in _plugins)
        {
            try
            {
                await plugin.ExecuteAsync(item, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(
                    ex,
                    "Plugin {PluginType} failed for WorkItem {WorkItemId}. ExternalId: {ExternalId}. SourceType: {SourceType}. Priority: {Priority}. CorrelationId: {CorrelationId}",
                    plugin.GetType().Name,
                    item.Id,
                    item.ExternalId,
                    item.SourceType,
                    item.Priority,
                    item.CorrelationId);
                item.MarkFaulted(ex.Message);
                return;
            }
        }

        item.MarkCompleted();
    }
}
