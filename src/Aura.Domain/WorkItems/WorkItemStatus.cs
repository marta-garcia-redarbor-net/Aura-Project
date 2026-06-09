namespace Aura.Domain.WorkItems;

/// <summary>
/// Represents the lifecycle stage of a <see cref="WorkItem"/>.
/// </summary>
public enum WorkItemStatus
{
    /// <summary>Work item created but not yet processed.</summary>
    Pending,

    /// <summary>Work item is currently being processed by the plugin pipeline.</summary>
    Processing,

    /// <summary>All plugins executed successfully.</summary>
    Completed,

    /// <summary>A plugin failed during execution.</summary>
    Faulted
}
