namespace Aura.Application.Models;

/// <summary>
/// The decision produced by the <see cref="Ports.IInterruptionPolicyEngine"/>
/// after evaluating all rules against a WorkItem.
/// </summary>
public sealed class InterruptionVerdict
{
    /// <summary>
    /// Whether the WorkItem should interrupt the user immediately or be queued for later.
    /// </summary>
    public InterruptionDecision Decision { get; }

    /// <summary>
    /// The name of the rule that triggered the InterruptNow decision, if any.
    /// </summary>
    public string? TriggerRule { get; }

    public string? Explanation { get; }

    public string? TargetUserId { get; }

    /// <summary>
    /// Full evaluation report containing results from ALL rules that ran.
    /// </summary>
    public EvaluationReport Report { get; }

    public InterruptionVerdict(
        InterruptionDecision decision,
        EvaluationReport report,
        string? triggerRule = null,
        string? explanation = null,
        string? targetUserId = null)
    {
        ArgumentNullException.ThrowIfNull(report);

        Decision = decision;
        Report = report;
        TriggerRule = triggerRule;
        Explanation = explanation;
        TargetUserId = targetUserId;
    }
}

/// <summary>
/// Whether the engine decided to interrupt the user or queue the notification.
/// </summary>
public enum InterruptionDecision
{
    /// <summary>Deliver the notification immediately — the user should see this now.</summary>
    InterruptNow,

    /// <summary>Queue the notification for later delivery (non-urgent).</summary>
    Queue,

    /// <summary>Defer delivery because the user context is currently unreceptive.</summary>
    Defer
}

/// <summary>
/// Aggregated report from all <see cref="Ports.IInterruptionRule"/> evaluations.
/// Contains the full set of rule results for dashboard/priority display.
/// </summary>
public sealed class EvaluationReport
{
    public IReadOnlyList<RuleResult> Results { get; }

    public EvaluationReport(IReadOnlyList<RuleResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        Results = results;
    }
}
