using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Options;
using Microsoft.Extensions.Options;

namespace Aura.Infrastructure.Adapters.Services.Rules;

/// <summary>
/// Rule that triggers when a WorkItem's preliminary score meets or exceeds the configured threshold.
/// Priority: 10 (evaluated first).
/// </summary>
public sealed class ScoreThresholdRule : IInterruptionRule
{
    private readonly InterruptionOptions _options;

    public ScoreThresholdRule(IOptions<InterruptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public int Priority => 10;

    public Task<RuleResult> EvaluateAsync(EvaluationContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (context.PriorityScore is not null)
        {
            return Task.FromResult(new RuleResult(
                ruleName: nameof(ScoreThresholdRule),
                matched: context.PriorityScore.IsInterruptCandidate,
                score: context.PriorityScore.InterruptionRank,
                confidence: 0.9,
                reason: $"Priority rule '{context.PriorityScore.RuleKey}' produced rank {context.PriorityScore.InterruptionRank}."));
        }

        // Try to extract the preliminary score from the WorkItem's metadata
        double score = 0;
        foreach (var key in context.Item.Metadata.Keys)
        {
            if (key.Contains("scoring.total", StringComparison.OrdinalIgnoreCase) ||
                key.EndsWith(".score", StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(context.Item.Metadata[key],
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsed))
                {
                    score = parsed;
                    break;
                }
            }
        }

        var matched = score >= _options.UrgentThreshold;
        var result = new RuleResult(
            ruleName: nameof(ScoreThresholdRule),
            matched: matched,
            score: score,
            confidence: 0.9,
            reason: matched
                ? $"Score {score:F1} meets or exceeds threshold {_options.UrgentThreshold:F1}"
                : $"Score {score:F1} is below threshold {_options.UrgentThreshold:F1}");

        return Task.FromResult(result);
    }
}
