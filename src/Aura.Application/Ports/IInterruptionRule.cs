using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// A single interruption rule that evaluates a WorkItem and produces a <see cref="RuleResult"/>.
/// Rules are registered in DI and executed by priority order.
/// </summary>
public interface IInterruptionRule
{
    /// <summary>
    /// Evaluates the work item context and returns a rule result.
    /// </summary>
    /// <param name="context">Evaluation context shared across rules.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="RuleResult"/> describing whether and why this rule matched.</returns>
    Task<RuleResult> EvaluateAsync(EvaluationContext context, CancellationToken ct);

    /// <summary>
    /// Priority determines execution order (lower = runs first).
    /// </summary>
    int Priority { get; }
}
