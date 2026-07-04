using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Application.Services;

public sealed class PriorityScoringService(IAlertRuleStore alertRuleStore) : IPriorityScoringService
{
    private readonly IAlertRuleStore _alertRuleStore = alertRuleStore ?? throw new ArgumentNullException(nameof(alertRuleStore));

    public PriorityScore Score(EvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var metadata = context.Item.Metadata;
        var factors = new List<PriorityFactorContribution>();

        if (context.TryGetBooleanSignal(WorkItemSignalKeys.ActionNeededSignal, out var actionNeeded) && actionNeeded)
        {
            factors.Add(new PriorityFactorContribution(WorkItemSignalKeys.ActionNeededSignal, "Explicit action needed signal is present."));
        }

        if (context.TryLevelSignal(WorkItemSignalKeys.TimeCriticalitySignal, out var timeCriticality) && timeCriticality is SignalLevel.High or SignalLevel.Critical)
        {
            factors.Add(new PriorityFactorContribution(WorkItemSignalKeys.TimeCriticalitySignal, $"Time criticality is {timeCriticality}."));
        }

        var sender = context.GetMetadataValue(WorkItemSignalKeys.CanonicalSender);
        var vipSenders = context.ApprovedPolicy?.VipSenders?.Count > 0
            ? context.ApprovedPolicy.VipSenders
            : _alertRuleStore.GetVipSendersAsync(CancellationToken.None).GetAwaiter().GetResult();

        if (!string.IsNullOrWhiteSpace(sender) && vipSenders.Contains(sender, StringComparer.OrdinalIgnoreCase))
        {
            factors.Add(new PriorityFactorContribution(WorkItemSignalKeys.VipSenderSignal, $"Sender '{sender}' is configured as VIP."));
        }

        var snippet = context.GetMetadataValue(WorkItemSignalKeys.CanonicalSnippet) ?? string.Empty;
        var keywords = context.ApprovedPolicy?.ActionNeededKeywords?.Count > 0
            ? context.ApprovedPolicy.ActionNeededKeywords
            : _alertRuleStore.GetKeywordsAsync(CancellationToken.None).GetAwaiter().GetResult();

        var matchedKeyword = keywords.FirstOrDefault(keyword => snippet.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(matchedKeyword))
        {
            factors.Add(new PriorityFactorContribution(WorkItemSignalKeys.CanonicalSnippet, $"Content cue '{matchedKeyword}' matched canonical snippet."));
        }

        if (factors.Any(f => f.Key == WorkItemSignalKeys.VipSenderSignal) && factors.Any(f => f.Key == WorkItemSignalKeys.ActionNeededSignal))
        {
            return new PriorityScore(
                RuleKey: "vip-action-needed",
                InterruptionRank: 120,
                IsInterruptCandidate: true,
                IsCriticalInterrupt: true,
                Explanation: $"VIP sender '{sender}' plus explicit action-needed signal.",
                Factors: factors);
        }

        if (factors.Any(f => f.Key == WorkItemSignalKeys.ActionNeededSignal) && factors.Any(f => f.Key == WorkItemSignalKeys.TimeCriticalitySignal))
        {
            return new PriorityScore(
                RuleKey: "urgent-action-needed",
                InterruptionRank: 100,
                IsInterruptCandidate: true,
                IsCriticalInterrupt: timeCriticality == SignalLevel.Critical,
                Explanation: "Urgent action-needed work matched the priority rules.",
                Factors: factors);
        }

        if (factors.Any(f => f.Key == WorkItemSignalKeys.VipSenderSignal))
        {
            return new PriorityScore(
                RuleKey: "vip-sender",
                InterruptionRank: 80,
                IsInterruptCandidate: true,
                IsCriticalInterrupt: false,
                Explanation: $"VIP sender '{sender}' increased interruption relevance.",
                Factors: factors);
        }

        if (!string.IsNullOrWhiteSpace(matchedKeyword))
        {
            return new PriorityScore(
                RuleKey: "content-cue",
                InterruptionRank: 40,
                IsInterruptCandidate: false,
                IsCriticalInterrupt: false,
                Explanation: $"Content cue '{matchedKeyword}' is traceable in the canonical snippet.",
                Factors: factors);
        }

        return new PriorityScore(
            RuleKey: "default-queue",
            InterruptionRank: 0,
            IsInterruptCandidate: false,
            IsCriticalInterrupt: false,
            Explanation: "No interrupt-priority rule matched the canonical signals.",
            Factors: factors);
    }
}
