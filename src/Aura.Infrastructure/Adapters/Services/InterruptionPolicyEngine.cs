using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging;
using Aura.Infrastructure.Adapters.LlmAdvisor;

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
    private readonly IInterruptionDecisionStore _decisionStore;
    private readonly IDecisionContextRetriever _decisionContextRetriever;
    private readonly ILlmDecisionAdvisor _llmDecisionAdvisor;
    private readonly ILogger<InterruptionPolicyEngine> _logger;

    public InterruptionPolicyEngine(
        IEnumerable<IInterruptionRule> rules,
        IFocusStateResolver focusStateResolver,
        IUserTriagePolicyProvider policyProvider,
        IPriorityScoringService priorityScoringService,
        IInterruptionDecisionStore decisionStore,
        ILogger<InterruptionPolicyEngine>? logger = null,
        IDecisionContextRetriever? decisionContextRetriever = null,
        ILlmDecisionAdvisor? llmDecisionAdvisor = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(focusStateResolver);
        ArgumentNullException.ThrowIfNull(policyProvider);
        ArgumentNullException.ThrowIfNull(priorityScoringService);
        ArgumentNullException.ThrowIfNull(decisionStore);

        _rules = rules.OrderBy(r => r.Priority).ToArray();
        _focusStateResolver = focusStateResolver;
        _policyProvider = policyProvider;
        _priorityScoringService = priorityScoringService;
        _decisionStore = decisionStore;
        _decisionContextRetriever = decisionContextRetriever ?? new Aura.Infrastructure.Adapters.Ingestion.SemanticIndex.NullDecisionContextRetriever();
        _llmDecisionAdvisor = llmDecisionAdvisor ?? new NullLlmDecisionAdvisor();
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

        // Check explicit overrides FIRST — they bypass scoring
        var overrideMatch = policy.ExplicitOverrides.FirstOrDefault(overrideRule =>
            overrideRule.AutoApply &&
            string.Equals(overrideRule.PatternKey, item.Metadata.TryGetValue(WorkItemSignalKeys.ExplicitPatternKey, out var pattern) ? pattern : null, StringComparison.OrdinalIgnoreCase));

        if (overrideMatch is not null)
        {
            var record = new InterruptionDecisionRecord(
                item.Id,
                item.Title,
                item.SourceType.ToString(),
                ToContractDecision(overrideMatch.Decision),
                null,
                overrideMatch.Reason,
                DateTimeOffset.UtcNow,
                focusState?.CurrentState.ToString() ?? "None",
                RetrievedSemanticContext: [],
                LlmRationale: "Explicit override applied before advisory.",
                GuardrailOutcome: "confirmed",
                UserOid: targetUserId);

            await _decisionStore.RecordAsync(record, ct);

            return new InterruptionVerdict(
                overrideMatch.Decision,
                new EvaluationReport([]),
                triggerRule: "ExplicitOverrideRule",
                explanation: overrideMatch.Reason,
                targetUserId: targetUserId);
        }

        if (targetUserId is null)
        {
            var record = new InterruptionDecisionRecord(
                item.Id,
                item.Title,
                item.SourceType.ToString(),
                ToContractDecision(InterruptionDecision.Queue),
                null,
                "No target user was resolved from canonical metadata, so interruption was not allowed.",
                DateTimeOffset.UtcNow,
                "None",
                RetrievedSemanticContext: [],
                LlmRationale: "No target user resolved; deterministic queue retained.",
                GuardrailOutcome: "confirmed",
                UserOid: targetUserId);

            await _decisionStore.RecordAsync(record, ct);

            return new InterruptionVerdict(
                InterruptionDecision.Queue,
                new EvaluationReport([]),
                triggerRule: null,
                explanation: "No target user was resolved from canonical metadata, so interruption was not allowed.",
                targetUserId: null);
        }

        var baseContext = new EvaluationContext(item, targetUserId, focusState, BuildNormalizedSignals(item, policy), null, policy);
        var priorityScore = _priorityScoringService.Score(baseContext);
        var context = new EvaluationContext(item, targetUserId, focusState, baseContext.NormalizedSignals, priorityScore, policy);
        var results = new List<RuleResult>();

        InterruptionDecision decision = focusState?.CurrentState == Aura.Domain.FocusState.FocusStateType.Away && !priorityScore.IsCriticalInterrupt
            ? InterruptionDecision.Defer
            : InterruptionDecision.Queue;
        string? triggerRule = null;

        if (decision == InterruptionDecision.Defer)
        {
            triggerRule = "FocusStateGate";
        }

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

        if (decision == InterruptionDecision.Queue && priorityScore.IsInterruptCandidate && focusState?.CurrentState == Aura.Domain.FocusState.FocusStateType.WindowOfOpportunity)
        {
            decision = InterruptionDecision.InterruptNow;
            triggerRule = priorityScore.RuleKey;
        }

        var deterministicDecision = decision;
        var explanation = BuildExplanation(priorityScore, focusState, results, deterministicDecision);

        var retrievalStart = DateTimeOffset.UtcNow;
        var retrievedContext = await _decisionContextRetriever.RetrieveAsync(item, ct);
        var retrievalMs = (DateTimeOffset.UtcNow - retrievalStart).TotalMilliseconds;

        var advisorStart = DateTimeOffset.UtcNow;
        var advisory = await _llmDecisionAdvisor.EvaluateAsync(
            new AdvisoryRequest(
                item,
                ToContractDecision(deterministicDecision),
                context.NormalizedSignals,
                retrievedContext),
            ct);
        var advisorMs = (DateTimeOffset.UtcNow - advisorStart).TotalMilliseconds;

        var guardrailOutcome = "confirmed";
        var finalDecision = deterministicDecision;

        var suggestedDecision = ParseContractDecision(advisory.SuggestedVerdict);
        var deterministicContract = ToContractDecision(deterministicDecision);
        var suggestionContract = suggestedDecision is null ? null : ToContractDecision(suggestedDecision.Value);

        if (string.Equals(advisory.GuardrailOutcome, "llm-unavailable", StringComparison.OrdinalIgnoreCase)
            || string.Equals(advisory.FailureReason, "timeout", StringComparison.OrdinalIgnoreCase)
            || suggestionContract is null)
        {
            guardrailOutcome = "llm-unavailable";
            finalDecision = deterministicDecision;
        }
        else if (string.Equals(suggestionContract, deterministicContract, StringComparison.OrdinalIgnoreCase))
        {
            guardrailOutcome = "confirmed";
            finalDecision = deterministicDecision;
        }
        else if (priorityScore.IsCriticalInterrupt)
        {
            guardrailOutcome = "blocked";
            finalDecision = deterministicDecision;
        }
        else
        {
            guardrailOutcome = "adjusted";
            finalDecision = suggestedDecision.Value;
        }

        Log.AdvisoryTrace(
            _logger,
            guardrailOutcome,
            retrievalMs,
            advisorMs,
            advisory.FailureReason);

        var finalRecord = new InterruptionDecisionRecord(
            item.Id,
            item.Title,
            item.SourceType.ToString(),
            ToContractDecision(finalDecision),
            priorityScore.InterruptionRank,
            explanation,
            DateTimeOffset.UtcNow,
            focusState?.CurrentState.ToString() ?? "None",
            RetrievedSemanticContext: retrievedContext,
            LlmRationale: advisory.Rationale,
            GuardrailOutcome: guardrailOutcome,
            UserOid: targetUserId);

        await _decisionStore.RecordAsync(finalRecord, ct);

        return new InterruptionVerdict(
            finalDecision,
            new EvaluationReport(results.AsReadOnly()),
            triggerRule,
            explanation: explanation,
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

        // Fallback to domain-level OwnerUserId property
        if (!string.IsNullOrWhiteSpace(item.OwnerUserId))
        {
            return item.OwnerUserId;
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

    private static string BuildExplanation(PriorityScore priorityScore, Aura.Domain.FocusState.FocusState? focusState, IReadOnlyList<RuleResult> results, InterruptionDecision decision)
    {
        var focusText = focusState is null ? "no resolved focus state" : $"focus state {focusState.CurrentState}";
        var matchedRules = results.Where(result => result.Matched).Select(result => result.RuleName).ToArray();
        var ruleText = matchedRules.Length == 0 ? "no rule matched" : string.Join(", ", matchedRules);
        return $"Decision {decision} from priority rule '{priorityScore.RuleKey}', {focusText}, and {ruleText}. {priorityScore.Explanation}";
    }

    private static string ToContractDecision(InterruptionDecision decision)
        => decision switch
        {
            InterruptionDecision.InterruptNow => "INTERRUPT",
            InterruptionDecision.Queue => "QUEUE",
            InterruptionDecision.Defer => "DEFER",
            _ => decision.ToString().ToUpperInvariant()
        };

    private static InterruptionDecision? ParseContractDecision(string? decision)
        => decision?.Trim().ToUpperInvariant() switch
        {
            "INTERRUPT" => InterruptionDecision.InterruptNow,
            "QUEUE" => InterruptionDecision.Queue,
            "DEFER" => InterruptionDecision.Defer,
            _ => null
        };

    private static partial class Log
    {
        [LoggerMessage(EventId = 4701, Level = LogLevel.Error,
            Message = "Interruption rule {RuleName} threw an exception during evaluation")]
        public static partial void RuleFailed(ILogger logger, string ruleName, Exception exception);

        [LoggerMessage(EventId = 4702, Level = LogLevel.Information,
            Message = "Decision advisory completed. guardrail={GuardrailOutcome} retrieval_ms={RetrievalLatencyMs} advisor_ms={AdvisorLatencyMs} fallback={FallbackReason}")]
        public static partial void AdvisoryTrace(
            ILogger logger,
            string guardrailOutcome,
            double retrievalLatencyMs,
            double advisorLatencyMs,
            string? fallbackReason);
    }
}
