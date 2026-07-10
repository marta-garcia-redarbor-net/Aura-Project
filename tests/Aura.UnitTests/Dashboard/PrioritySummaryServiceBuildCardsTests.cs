using Aura.UI.Models;
using Aura.UI.Services;

namespace Aura.UnitTests.Dashboard;

public class PrioritySummaryServiceBuildCardsTests
{
    private static readonly DashboardPreviewResponse EmptyPreview = new(
        InboxGroups: Array.Empty<InboxSourceGroupResponse>(),
        SummaryEntries: Array.Empty<SummaryPreviewEntryResponse>());

    private static readonly IReadOnlyList<UpcomingMeetingResponse> EmptyCalendar =
        Array.Empty<UpcomingMeetingResponse>();

    private static PullRequestResponse CreatePr(int id, string attentionScope)
    {
        return new PullRequestResponse(
            Id: id,
            Title: $"PR #{id}",
            RepoName: "repo",
            Author: "Alice",
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            Status: "active",
            ReviewerCount: 1,
            CommentCount: 0,
            FileCount: 1,
            SourceLink: $"https://dev.azure.com/pr/{id}",
            IsDraft: false,
            Priority: "high",
            BranchName: "main",
            SourceBranchName: "feature",
            BuildStatus: "passing",
            ReviewApprovals: 0,
            ReviewRequired: 1,
            ReviewChangesRequested: 0,
            AttentionScope: attentionScope);
    }

    [Fact]
    public void BuildCards_FiltersOutUnknownScope_KeepsDirectGroupBoth()
    {
        var prs = new List<PullRequestResponse>
        {
            CreatePr(1, "direct"),
            CreatePr(2, "direct"),
            CreatePr(3, "group"),
            CreatePr(4, "both"),
            CreatePr(5, "unknown")
        };

        var cards = PrioritySummaryService.BuildCards(EmptyPreview, EmptyCalendar, prs);

        var prCard = cards.First(c => c.IsPrCard);
        Assert.Equal(4, prCard.PrItems!.Count);
        Assert.DoesNotContain(prCard.PrItems, item => item.AttentionScope == "unknown");
    }

    [Fact]
    public void BuildCards_AllUnknownOrEmpty_ReturnsZeroPrItems()
    {
        var allUnknown = new List<PullRequestResponse>
        {
            CreatePr(1, "unknown"),
            CreatePr(2, "unknown")
        };

        var cardsWithUnknown = PrioritySummaryService.BuildCards(EmptyPreview, EmptyCalendar, allUnknown);
        var prCardUnknown = cardsWithUnknown.First(c => c.IsPrCard);
        Assert.Empty(prCardUnknown.PrItems!);

        var cardsWithEmpty = PrioritySummaryService.BuildCards(EmptyPreview, EmptyCalendar, Array.Empty<PullRequestResponse>());
        var prCardEmpty = cardsWithEmpty.First(c => c.IsPrCard);
        Assert.Empty(prCardEmpty.PrItems!);
    }

    [Fact]
    public void BuildCards_PropagatesAttentionScope_ToPreviewItemResponse()
    {
        var prs = new List<PullRequestResponse>
        {
            CreatePr(10, "direct"),
            CreatePr(20, "group"),
            CreatePr(30, "both")
        };

        var cards = PrioritySummaryService.BuildCards(EmptyPreview, EmptyCalendar, prs);

        var prCard = cards.First(c => c.IsPrCard);
        Assert.Equal(3, prCard.PrItems!.Count);

        var directItem = prCard.PrItems.First(i => i.Title == "PR #10");
        Assert.Equal("direct", directItem.AttentionScope);

        var groupItem = prCard.PrItems.First(i => i.Title == "PR #20");
        Assert.Equal("group", groupItem.AttentionScope);

        var bothItem = prCard.PrItems.First(i => i.Title == "PR #30");
        Assert.Equal("both", bothItem.AttentionScope);
    }
}
