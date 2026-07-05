using Aura.Application.Models;
using Aura.Application.UseCases.MorningSummary;
using Aura.Domain.WorkItems;
using System.Reflection;

namespace Aura.UnitTests.Triage;

public sealed class MorningSummaryRankingPolicyTests
{
    [Fact]
    public void Rank_DeadlineResolvesBeforeImpactAndRisk()
    {
        var policy = new MorningSummaryRankingPolicy();

        var withDeadline = CreateWorkItem(
            "a",
            WorkItemPriority.Low,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookDeadlineCue] = "due tomorrow",
                [WorkItemSignalKeys.OutlookDeadlineSource] = "subject"
            });

        var highImpactNoDeadline = CreateWorkItem("b", WorkItemPriority.Critical);

        var ranked = policy.Rank([highImpactNoDeadline, withDeadline]);

        Assert.Equal("a", ranked[0].Item.ExternalId);
        Assert.Equal(RankingFactor.Deadline, ranked[0].Explanation.Contributions[0].Factor);
    }

    [Fact]
    public void Rank_ImpactResolvesWhenDeadlineDoesNot()
    {
        var policy = new MorningSummaryRankingPolicy();

        var highImpact = CreateWorkItem("a", WorkItemPriority.Critical);
        var lowImpact = CreateWorkItem("b", WorkItemPriority.Low);

        var ranked = policy.Rank([lowImpact, highImpact]);

        Assert.Equal("a", ranked[0].Item.ExternalId);
        Assert.Equal(RankingFactor.Impact, ranked[0].Explanation.Contributions[0].Factor);
    }

    [Fact]
    public void Rank_RiskResolvesWhenDeadlineAndImpactDoNot()
    {
        var policy = new MorningSummaryRankingPolicy();

        var highRisk = CreateWorkItem(
            "a",
            WorkItemPriority.Medium,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.TeamsPriorityRaw] = "absent",
                ["triage.risk.score"] = "0.90"
            });

        var lowRisk = CreateWorkItem(
            "b",
            WorkItemPriority.Medium,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.TeamsPriorityRaw] = "absent",
                ["triage.risk.score"] = "0.10"
            });

        var ranked = policy.Rank([lowRisk, highRisk]);

        Assert.Equal("a", ranked[0].Item.ExternalId);
        Assert.Equal(RankingFactor.Risk, ranked[0].Explanation.Contributions[0].Factor);
    }

    [Fact]
    public void Rank_PreliminaryScoreBreaksPostExplicitTie_AndAppearsAsSingleInput()
    {
        var policy = new MorningSummaryRankingPolicy();

        var first = CreateWorkItem(
            "a",
            WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookScoringTotalScore] = "0.85"
            });

        var second = CreateWorkItem(
            "b",
            WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookScoringTotalScore] = "0.45"
            });

        var ranked = policy.Rank([second, first]);

        Assert.Equal("a", ranked[0].Item.ExternalId);
        var preliminaryContributions = ranked[0].Explanation.Contributions
            .Where(c => c.Factor == RankingFactor.PreliminaryScore)
            .ToList();

        Assert.Single(preliminaryContributions);
    }

    [Fact]
    public void Rank_PreliminaryScoreIsFallbackWhenAllExplicitSignalsAreAbsent()
    {
        var policy = new MorningSummaryRankingPolicy();

        var withScore = CreateWorkItem(
            "a",
            WorkItemPriority.Medium,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.TeamsPriorityRaw] = "absent",
                [WorkItemSignalKeys.OutlookScoringTotalScore] = "0.80"
            });

        var withoutScore = CreateWorkItem(
            "b",
            WorkItemPriority.Medium,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.TeamsPriorityRaw] = "absent"
            });

        var ranked = policy.Rank([withoutScore, withScore]);

        Assert.Equal("a", ranked[0].Item.ExternalId);
        Assert.Contains(ranked[0].Explanation.Contributions, c => c.Factor == RankingFactor.PreliminaryScore);
    }

    [Fact]
    public void Rank_TieChain_UsesNearestDueDate_ThenOldestCreatedAt_ThenLexicalExternalId()
    {
        var policy = new MorningSummaryRankingPolicy();

        var laterDue = CreateWorkItem(
            "b",
            WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookScoringTotalScore] = "0.50",
                ["outlook.deadline.atUtc"] = "2026-06-23T09:00:00Z"
            });

        var earlierDue = CreateWorkItem(
            "a",
            WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookScoringTotalScore] = "0.50",
                ["outlook.deadline.atUtc"] = "2026-06-22T09:00:00Z"
            });

        var dueResolved = policy.Rank([laterDue, earlierDue]);
        Assert.Equal("a", dueResolved[0].Item.ExternalId);

        var oldCreated = CreateWorkItem(
            "c",
            WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookScoringTotalScore] = "0.50"
            },
            capturedAt: new DateTimeOffset(2026, 6, 20, 8, 0, 0, TimeSpan.Zero));
        SetCreatedAt(oldCreated, new DateTimeOffset(2026, 6, 20, 8, 0, 0, TimeSpan.Zero));

        var newCreated = CreateWorkItem(
            "d",
            WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookScoringTotalScore] = "0.50"
            },
            capturedAt: new DateTimeOffset(2026, 6, 21, 8, 0, 0, TimeSpan.Zero));
        SetCreatedAt(newCreated, new DateTimeOffset(2026, 6, 21, 8, 0, 0, TimeSpan.Zero));

        var createdAtResolved = policy.Rank([newCreated, oldCreated]);
        Assert.Equal("c", createdAtResolved[0].Item.ExternalId);

        var idB = CreateWorkItem(
            "b-lex",
            WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookScoringTotalScore] = "0.50"
            },
            capturedAt: new DateTimeOffset(2026, 6, 20, 8, 0, 0, TimeSpan.Zero));
        SetCreatedAt(idB, new DateTimeOffset(2026, 6, 20, 8, 0, 0, TimeSpan.Zero));

        var idA = CreateWorkItem(
            "a-lex",
            WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookScoringTotalScore] = "0.50"
            },
            capturedAt: new DateTimeOffset(2026, 6, 20, 8, 0, 0, TimeSpan.Zero));
        SetCreatedAt(idA, new DateTimeOffset(2026, 6, 20, 8, 0, 0, TimeSpan.Zero));

        var lexicalResolved = policy.Rank([idB, idA]);
        Assert.Equal("a-lex", lexicalResolved[0].Item.ExternalId);
    }

    [Fact]
    public void Rank_InsufficientSignalsItems_ArePlacedLast_WithEmptyContributions()
    {
        var policy = new MorningSummaryRankingPolicy();

        var withSignal = CreateWorkItem(
            "with-signal",
            WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookScoringTotalScore] = "0.30"
            });

        var insufficient = CreateWorkItem(
            "insufficient",
            WorkItemPriority.Medium,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.TeamsPriorityRaw] = "absent"
            });

        var ranked = policy.Rank([insufficient, withSignal]);

        Assert.Equal("with-signal", ranked[0].Item.ExternalId);
        Assert.Equal("insufficient", ranked[1].Item.ExternalId);
        Assert.Empty(ranked[1].Explanation.Contributions);
    }

    private static WorkItem CreateWorkItem(
        string externalId,
        WorkItemPriority priority,
        IReadOnlyDictionary<string, string>? metadata = null,
        DateTimeOffset? capturedAt = null)
    {
        return new WorkItem(
            externalId,
            $"Item {externalId}",
            "teams",
            WorkItemSourceType.TeamsMessage,
            priority,
            metadata ?? new Dictionary<string, string>(),
            correlationId: $"corr-{externalId}",
            capturedAtUtc: capturedAt ?? new DateTimeOffset(2026, 6, 21, 8, 0, 0, TimeSpan.Zero));
    }

    private static void SetCreatedAt(WorkItem item, DateTimeOffset value)
    {
        var field = typeof(WorkItem).GetField("<CreatedAt>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(item, value);
    }
}
