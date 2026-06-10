using Aura.Domain.WorkItems;

namespace Aura.Application.Kernel;

/// <summary>
/// Contract for a kernel plugin that processes a <see cref="WorkItem"/>.
/// Plugins execute sequentially within the <see cref="IPluginRegistry"/> pipeline.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Processes a work item. Implementations should be idempotent when possible.
    /// </summary>
    Task ExecuteAsync(WorkItem item, CancellationToken ct);
}
