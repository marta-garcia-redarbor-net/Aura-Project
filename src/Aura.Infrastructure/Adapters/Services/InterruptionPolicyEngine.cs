using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.FocusState;
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
    private readonly IFocusStateResolver _focusStateResolver;
    private readonly IUserTriagePolicyProvider _policyProvider;
    private readonly IPriorityScoringService _priorityScoringService;
    private readonly ILogger<InterruptionPolicyEngine> _logger;

    public InterruptionPolicyEngine(
        IEnumerable<IInterruptionRule> rules,
        IFocusStateResolver focusStateResolver,
        IUserTriagePolicyProvider policyProvider,
        IPriorityScoringService priorityScoringService,
        ILogger<InterruptionPolicyEngine>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(focusStateResolver);
        ArgumentNullException.ThrowIfNull(policyProvider);
        ArgumentNullException.ThrowIfNull(priorityScoringService);

        _rules = rules.OrderBy(r => r.Priority).ToArray();
        _focusStateResolver = focusStateResolver;
        _policyProvider = policyProvider;
        _priorityScoringService = priorityScoringService;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<InterruptionPolicyEngine>.Instance;
    }

    public async Task<InterruptionVerdict> EvaluateAsync(WorkItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ct.ThrowIfCancellationRequested();

        var targetUserId = ResolveTargetUserId(item);
        var focusState = targetUserId is null
            ? null
            : await _focusStateResolver.ResolveAsync(targetUserId, ct);
        var policy = targetUserId is null
            ? UserTriagePolicy.Empty
            : await _policyProvider.GetApprovedPolicyAsync(targetUserId, ct);

        var baseContext = new EvaluationContext(item, targetUserId, focusState, BuildNormalizedSignals(item, policy), null, policy);
        var priorityScore = _priorityScoringService.Score(baseContext);
        var context = new EvaluationContext(item, targetUserId, focusState, baseContext.NormalizedSignals, priorityScore, policy);
        var results = new List<RuleResult>();

        var overrideMatch = policy.ExplicitOverrides.FirstOrDefault(overrideRule =>
            overrideRule.AutoApply &&
            string.Equals(overrideRule.PatternKey, item.Metadata.TryGetValue(WorkItemSignalKeys.ExplicitPatternKey, out var pattern) ? pattern : null, StringComparison.OrdinalIgnoreCase));

        if (overrideMatch is not null)
        {
            return new InterruptionVerdict(
                overrideMatch.Decision,
                new EvaluationReport([]),
                triggerRule: "ExplicitOverrideRule",
                explanation: overrideMatch.Reason,
                targetUserId: targetUserId);
        }

        if (targetUserId is null)
        {
            return new InterruptionVerdict(
                InterruptionDecision.Queue,
                new EvaluationReport([]),
                triggerRule: null,
                explanation: "No target user was resolved from canonical metadata, so interruption was not allowed.",
                targetUserId: null);
        }

        if (focusState?.CurrentState == FocusStateType.Away && !priorityScore.IsCriticalInterrupt)
        {
            return new InterruptionVerdict(
                InterruptionDecision.Defer,
                new EvaluationReport([]),
                triggerRule: "FocusStateGate",
                explanation: $"Focus state {focusState.CurrentState} defers non-critical interruption after evaluating '{priorityScore.RuleKey}'.",
                targetUserId: targetUserId);
        }

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

        if (decision == InterruptionDecision.Queue && priorityScore.IsInterruptCandidate && focusState?.CurrentState == FocusStateType.WindowOfOpportunity)
        {
            decision = InterruptionDecision.InterruptNow;
            triggerRule = priorityScore.RuleKey;
        }

        return new InterruptionVerdict(
            decision,
            new EvaluationReport(results.AsReadOnly()),
            triggerRule,
            explanation: BuildExplanation(priorityScore, focusState, results, decision),
            targetUserId: targetUserId);
    }

    private static string? ResolveTargetUserId(WorkItem item)
    {
        if (item.Metadata.TryGetValue("assignedTo", out var assignedTo) && !string.IsNullOrWhiteSpace(assignedTo))
        {
            return assignedTo;
        }

        if (item.Metadata.TryGetValue(WorkItemSignalKeys.TargetResponsibleUserId, out var responsibleUserId) && !string.IsNullOrWhiteSpace(responsibleUserId))
        {
            return responsibleUserId;
        }

        if (item.Metadata.TryGetValue(WorkItemSignalKeys.TargetOwnerUserId, out var ownerUserId) && !string.IsNullOrWhiteSpace(ownerUserId))
        {
            return ownerUserId;
        }

        return null;
    }

    private static IReadOnlyDictionary<string, NormalizedSignal> BuildNormalizedSignals(WorkItem item, UserTriagePolicy policy)
    {
        var signals = new Dictionary<string, NormalizedSignal>(StringComparer.OrdinalIgnoreCase);
        var metadata = item.Metadata;

        if (metadata.TryGetValue(WorkItemSignalKeys.CanonicalSender, out var sender)
            && !string.IsNullOrWhiteSpace(sender)
            && policy.VipSenders.Contains(sender, StringComparer.OrdinalIgnoreCase))
        {
            signals[WorkItemSignalKeys.VipSenderSignal] = new BooleanSignal(WorkItemSignalKeys.VipSenderSignal, true, $"Sender '{sender}' is configured as VIP.");
        }

        if (metadata.TryGetValue(WorkItemSignalKeys.ActionNeededSignal, out var actionNeededRaw)
            && bool.TryParse(actionNeededRaw, out var actionNeeded))
        {
            signals[WorkItemSignalKeys.ActionNeededSignal] = new BooleanSignal(WorkItemSignalKeys.ActionNeededSignal, actionNeeded, "Action-needed signal normalized from canonical metadata.");
        }

        if (metadata.TryGetValue(WorkItemSignalKeys.TimeCriticalitySignal, out var timeCriticalityRaw)
            && Enum.TryParse<SignalLevel>(timeCriticalityRaw, true, out var timeCriticality))
        {
            signals[WorkItemSignalKeys.TimeCriticalitySignal] = new LevelSignal(WorkItemSignalKeys.TimeCriticalitySignal, timeCriticality, "Time criticality normalized from canonical metadata.");
        }

        if (metadata.TryGetValue(WorkItemSignalKeys.MessageLengthBucketSignal, out var bucket) && !string.IsNullOrWhiteSpace(bucket))
        {
            signals[WorkItemSignalKeys.MessageLengthBucketSignal] = new LevelSignal(WorkItemSignalKeys.MessageLengthBucketSignal, bucket.Equals("short", StringComparison.OrdinalIgnoreCase) ? SignalLevel.High : SignalLevel.Low, $"Message length bucket '{bucket}' normalized.");
        }

        return signals;
    }

    private static string BuildExplanation(PriorityScore priorityScore, FocusState? focusState, IReadOnlyList<RuleResult> results, InterruptionDecision decision)
    {
        var focusText = focusState is null ? "no resolved focus state" : $"focus state {focusState.CurrentState}";
        var matchedRules = results.Where(result => result.Matched).Select(result => result.RuleName).ToArray();
        var ruleText = matchedRules.Length == 0 ? "no rule matched" : string.Join(", ", matchedRules);
        return $"Decision {decision} from priority rule '{priorityScore.RuleKey}', {focusText}, and {ruleText}. {priorityScore.Explanation}";
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 4701, Level = LogLevel.Error,
            Message = "Interruption rule {RuleName} threw an exception during evaluation")]
        public static partial void RuleFailed(ILogger logger, string ruleName, Exception exception);
    }
}
