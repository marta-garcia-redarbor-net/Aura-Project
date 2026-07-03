using Aura.Domain.WorkItems;

namespace Aura.Application.Models;

/// <summary>
/// Context passed to each <see cref="Ports.IInterruptionRule"/> during evaluation.
/// Carries the WorkItem being evaluated and may be extended with cross-cutting state in the future.
/// </summary>
public sealed class EvaluationContext
{
    /// <summary>
    /// The WorkItem being evaluated by the interruption policy engine.
    /// </summary>
    public WorkItem Item { get; }

    public EvaluationContext(WorkItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Item = item;
    }
}
