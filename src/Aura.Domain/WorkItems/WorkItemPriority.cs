namespace Aura.Domain.WorkItems;

/// <summary>
/// Canonical priority for a <see cref="WorkItem"/>.
/// </summary>
public enum WorkItemPriority
{
    Critical,
    High,
    Medium,
    Low
}

/// <summary>
/// Extension methods for <see cref="WorkItemPriority"/>.
/// </summary>
public static class WorkItemPriorityExtensions
{
    /// <summary>
    /// Returns the default numeric score for a <see cref="WorkItemPriority"/>.
    /// </summary>
    public static int GetDefaultScore(this WorkItemPriority p) => p switch
    {
        WorkItemPriority.Critical => 100,
        WorkItemPriority.High => 75,
        WorkItemPriority.Medium => 50,
        WorkItemPriority.Low => 25,
        _ => 0
    };
}
