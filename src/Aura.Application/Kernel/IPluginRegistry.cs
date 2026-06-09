using Aura.Domain.WorkItems;

namespace Aura.Application.Kernel;

/// <summary>
/// Orchestrates sequential plugin execution against a <see cref="WorkItem"/>.
/// </summary>
public interface IPluginRegistry
{
    /// <summary>
    /// Executes all registered plugins sequentially against the given work item.
    /// On plugin failure, aborts remaining plugins and marks the work item as faulted.
    /// </summary>
    Task ExecuteAsync(WorkItem item, CancellationToken ct);
}
