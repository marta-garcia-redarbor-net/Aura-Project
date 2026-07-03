using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Adapters.Services;

/// <summary>
/// Evaluates a WorkItem against all registered <see cref="IInterruptionRule"/> instances,
/// ordered by priority. Short-circuits on the first rule that returns <c>matched=true</c>
/// for the InterruptNow decision, but ALL rules still run to populate the EvaluationReport.
/// </summary>
public sealed partial class InterruptionPolicyEngine : IInterruptionPolicyEngine
{
    private readonly IReadOnlyList<IInterruptionRule> _rules;
    private readonly ILogger<InterruptionPolicyEngine> _logger;

    public InterruptionPolicyEngine(
        IEnumerable<IInterruptionRule> rules,
        ILogger<InterruptionPolicyEngine>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(rules);

        _rules = rules.OrderBy(r => r.Priority).ToArray();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<InterruptionPolicyEngine>.Instance;
    }

    public async Task<InterruptionVerdict> EvaluateAsync(WorkItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ct.ThrowIfCancellationRequested();

        var context = new EvaluationContext(item);
        var results = new List<RuleResult>();
        InterruptionDecision decision = InterruptionDecision.Queue;
        string? triggerRule = null;

        foreach (var rule in _rules)
        {
            ct.ThrowIfCancellationRequested();

            RuleResult result;
            try
            {
                result = await rule.EvaluateAsync(context, ct);
            }
            catch (Exception ex)
            {
                Log.RuleFailed(_logger, rule.GetType().Name, ex);
                result = new RuleResult(rule.GetType().Name, false, 0, 0, $"Rule threw: {ex.Message}");
            }

            results.Add(result);

            // First match determines interruption decision (but continue for full report)
            if (decision == InterruptionDecision.Queue && result.Matched)
            {
                decision = InterruptionDecision.InterruptNow;
                triggerRule = result.RuleName;
            }
        }

        return new InterruptionVerdict(decision, new EvaluationReport(results.AsReadOnly()), triggerRule);
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 4701, Level = LogLevel.Error,
            Message = "Interruption rule {RuleName} threw an exception during evaluation")]
        public static partial void RuleFailed(ILogger logger, string ruleName, Exception exception);
    }
}
