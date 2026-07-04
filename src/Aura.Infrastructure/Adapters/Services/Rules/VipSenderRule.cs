using Aura.Application.Models;
using Aura.Application.Ports;


namespace Aura.Infrastructure.Adapters.Services.Rules;

/// <summary>
/// Rule that triggers when a WorkItem originates from a VIP sender.
/// Priority: 20.
/// </summary>
public sealed class VipSenderRule : IInterruptionRule
{
    private readonly IAlertRuleStore _ruleStore;

    public VipSenderRule(IAlertRuleStore ruleStore)
    {
        ArgumentNullException.ThrowIfNull(ruleStore);
        _ruleStore = ruleStore;
    }

    public int Priority => 20;

    public async Task<RuleResult> EvaluateAsync(EvaluationContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (context.TryGetBooleanSignal(WorkItemSignalKeys.VipSenderSignal, out var isVip) && isVip)
        {
            return new RuleResult(nameof(VipSenderRule), true, 8.0, 0.95, "Typed VIP sender signal matched.");
        }

        // Extract sender email from metadata
        var sender = GetSenderFromMetadata(context.Item.Metadata);
        if (string.IsNullOrWhiteSpace(sender))
        {
            return new RuleResult(
                nameof(VipSenderRule), false, 0, 0.8,
                "No sender metadata found");
        }

        var vipSenders = await _ruleStore.GetVipSendersAsync(ct);
        var matched = vipSenders.Contains(sender, StringComparer.OrdinalIgnoreCase);

        return new RuleResult(
            nameof(VipSenderRule),
            matched,
            matched ? 8.0 : 0,
            0.9,
            matched
                ? $"Sender '{sender}' is a VIP"
                : $"Sender '{sender}' is not in VIP list");
    }

    private static string? GetSenderFromMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        // Check common metadata keys for sender/from information
        foreach (var key in metadata.Keys)
        {
            if (key.Contains("from", StringComparison.OrdinalIgnoreCase) &&
                !key.Contains("subject", StringComparison.OrdinalIgnoreCase) &&
                !key.Contains("date", StringComparison.OrdinalIgnoreCase))
            {
                return metadata[key];
            }
        }

        return null;
    }
}
