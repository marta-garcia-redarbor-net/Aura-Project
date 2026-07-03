namespace Aura.Application.Models;

/// <summary>
/// The outcome of evaluating a single <see cref="Ports.IInterruptionRule"/> against a WorkItem.
/// </summary>
public sealed class RuleResult
{
    /// <summary>The unique name of the rule that produced this result.</summary>
    public string RuleName { get; }

    /// <summary>Whether the rule matched the WorkItem.</summary>
    public bool Matched { get; }

    /// <summary>A numeric score indicating urgency (higher = more urgent).</summary>
    public double Score { get; }

    /// <summary>Confidence level in the rule's assessment (0.0 to 1.0).</summary>
    public double Confidence { get; }

    /// <summary>Human-readable explanation of why this rule produced its verdict.</summary>
    public string? Reason { get; }

    public RuleResult(
        string ruleName,
        bool matched,
        double score,
        double confidence,
        string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(ruleName))
            throw new ArgumentException("RuleName must not be null or empty.", nameof(ruleName));

        RuleName = ruleName;
        Matched = matched;
        Score = score;
        Confidence = confidence;
        Reason = reason;
    }
}
