using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;

namespace Aura.Infrastructure.Adapters.Demo;

/// <summary>
/// Demo-mode implementation of <see cref="IDecisionContextRetriever"/> that returns
/// synthetic semantic context items based on the work item's title and source type.
/// This gives the decision log realistic "Retrieved Semantic Context" entries
/// without requiring Qdrant or a populated semantic outbox.
/// </summary>
internal sealed class DemoDecisionContextRetriever : IDecisionContextRetriever
{
    private static readonly Random Rng = Random.Shared;

    private static readonly string[] ContextSnippets =
    [
        "Team decided to prioritize this item during the weekly triage meeting. Related PR #4128 was approved and deployed to staging.",
        "Previous discussion in #architecture channel: similar issue was resolved by adding connection pooling. See ADR-0042.",
        "Customer impact analysis shows this affects approximately 200 users. Support ticket #8843 escalated by account team.",
        "Related incident I-7732 was resolved last week. Runbook published in Confluence under 'Infrastructure Runbooks'.",
        "Engineering team discussed this in the last sprint retro. Action item: review and prioritize for Sprint 13.",
        "Security advisory published. CVSS score 7.5. Patches available for versions >= 2.4.0. Upgrade window requested.",
        "Dependency update available: lodash 4.17.21 fixes the reported vulnerability. Breaking changes reviewed by team lead.",
        "Build pipeline failing on main branch since commit a3f2c91. Rollback considered. Team notified in #engineering channel."
    ];

    private static readonly string[] SourceTypes = ["SlackMessage", "OutlookEmail", "TeamsMessage", "GitHubIssue", "ConfluencePage"];

    public Task<IReadOnlyList<DecisionContextItem>> RetrieveAsync(WorkItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ct.ThrowIfCancellationRequested();

        var count = Rng.Next(1, 4);
        var items = new List<DecisionContextItem>(count);

        for (var i = 0; i < count; i++)
        {
            var snippet = ContextSnippets[Rng.Next(ContextSnippets.Length)];
            var sourceType = SourceTypes[Rng.Next(SourceTypes.Length)];
            var score = Math.Round(0.5 + Rng.NextDouble() * 0.5, 2);

            items.Add(new DecisionContextItem(
                CanonicalSourceId: $"demo-ctx-{Guid.NewGuid().ToString("N")[..8]}",
                ContentSnippet: snippet,
                SourceType: sourceType,
                RelevanceScore: score));
        }

        return Task.FromResult<IReadOnlyList<DecisionContextItem>>(items.AsReadOnly());
    }
}
