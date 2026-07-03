using Aura.Application.Models;
using Aura.Domain.WorkItems;

namespace Aura.Application.Ports;

/// <summary>
/// Evaluates a WorkItem against all registered interruption rules and produces
/// a verdict on whether to interrupt the user or queue the notification.
/// </summary>
public interface IInterruptionPolicyEngine
{
    /// <summary>
    /// Evaluates the specified WorkItem against all rules.
    /// </summary>
    /// <param name="item">The WorkItem to evaluate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An <see cref="InterruptionVerdict"/> with the decision and full evaluation report.</returns>
    Task<InterruptionVerdict> EvaluateAsync(WorkItem item, CancellationToken ct);
}
