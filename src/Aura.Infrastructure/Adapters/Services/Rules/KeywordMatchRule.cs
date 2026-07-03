using Aura.Application.Models;
using Aura.Application.Ports;


namespace Aura.Infrastructure.Adapters.Services.Rules;

/// <summary>
/// Rule that triggers when a WorkItem's title or metadata contains any configured alert keywords.
/// Priority: 30.
/// </summary>
public sealed class KeywordMatchRule : IInterruptionRule
{
    private readonly IAlertRuleStore _ruleStore;

    public KeywordMatchRule(IAlertRuleStore ruleStore)
    {
        ArgumentNullException.ThrowIfNull(ruleStore);
        _ruleStore = ruleStore;
    }

    public int Priority => 30;

    public async Task<RuleResult> EvaluateAsync(EvaluationContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var keywords = await _ruleStore.GetKeywordsAsync(ct);
        if (keywords.Count == 0)
        {
            return new RuleResult(
                nameof(KeywordMatchRule), false, 0, 1.0,
                "No keywords configured");
        }

        // Collect all text fields to search
        var searchTexts = new List<string>
        {
            context.Item.Title
        };

        // Add metadata fields that might contain text
        foreach (var key in context.Item.Metadata.Keys)
        {
            if (key.Contains("subject", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("body", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("text", StringComparison.OrdinalIgnoreCase))
            {
                searchTexts.Add(context.Item.Metadata[key]);
            }
        }

        var matchedKeyword = FindMatchingKeyword(searchTexts, keywords);
        var matched = matchedKeyword is not null;

        return new RuleResult(
            nameof(KeywordMatchRule),
            matched,
            matched ? 7.0 : 0,
            0.85,
            matched
                ? $"Matched keyword '{matchedKeyword}' in content"
                : "No keywords matched");
    }

    private static string? FindMatchingKeyword(
        IReadOnlyList<string> searchTexts,
        IReadOnlyList<string> keywords)
    {
        foreach (var text in searchTexts)
        {
            foreach (var keyword in keywords)
            {
                if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return keyword;
                }
            }
        }

        return null;
    }
}
