using Aura.UI.Components.Dashboard;
using Aura.UI.Models;

namespace Aura.UnitTests.Dashboard;

public class PriorityPresentationTests
{
    [Theory]
    [InlineData(95, true)]
    [InlineData(75, true)]
    [InlineData(74, false)]
    [InlineData(null, false)]
    public void IsHighPriority_UsesThreshold75(int? score, bool expected)
    {
        var result = PriorityPresentation.IsHighPriority(score);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void SelectTopPriority_WithTieAtBoundary_IncludesAllTiedItems()
    {
        var items = new[]
        {
            new InboxItemPreviewResponse("A", "messages", "1m", 1, "Review") { PriorityScore = 95 },
            new InboxItemPreviewResponse("B", "messages", "2m", 1, "Review") { PriorityScore = 90 },
            new InboxItemPreviewResponse("C", "messages", "3m", 1, "Review") { PriorityScore = 85 },
            new InboxItemPreviewResponse("D", "messages", "4m", 1, "Review") { PriorityScore = 85 },
            new InboxItemPreviewResponse("E", "messages", "5m", 1, "Review") { PriorityScore = 70 }
        };

        var top = PriorityPresentation.SelectTopPriority(items).ToList();

        Assert.Equal(4, top.Count);
        Assert.DoesNotContain(items[4], top);
    }

    [Fact]
    public void SortWorkItemsForTopPriority_OrdersByPriorityScoreDesc_ThenCapturedAtDesc()
    {
        var items = new[]
        {
            new WorkItemDetailResponse(Guid.NewGuid(), "outlook-1", "Outlook medium", "outlook", "OutlookEmail", "Pending", "Medium", "1h", new DateTimeOffset(2026, 7, 5, 10, 0, 0, TimeSpan.Zero)) { PriorityScore = 50 },
            new WorkItemDetailResponse(Guid.NewGuid(), "teams-2", "Teams top newer", "teams", "TeamsMessage", "Pending", "High", "2m", new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero)) { PriorityScore = 80 },
            new WorkItemDetailResponse(Guid.NewGuid(), "teams-1", "Teams top older", "teams", "TeamsMessage", "Pending", "High", "5m", new DateTimeOffset(2026, 7, 5, 11, 0, 0, TimeSpan.Zero)) { PriorityScore = 80 },
            new WorkItemDetailResponse(Guid.NewGuid(), "pr-1", "PR low", "pr", "PrReview", "Pending", "Low", "3h", new DateTimeOffset(2026, 7, 5, 9, 0, 0, TimeSpan.Zero)) { PriorityScore = 20 }
        };

        var ordered = PriorityPresentation.SortWorkItemsForTopPriority(items).ToList();

        Assert.Equal("Teams top newer", ordered[0].Title);
        Assert.Equal("Teams top older", ordered[1].Title);
        Assert.Equal("Outlook medium", ordered[2].Title);
        Assert.Equal("PR low", ordered[3].Title);
    }

    [Fact]
    public void GetHighPriorityCount_CountsOnlyScoresGreaterThanOrEqualTo75()
    {
        var items = new[]
        {
            new InboxItemPreviewResponse("A", "messages", "1m", 1, "Review") { PriorityScore = 100 },
            new InboxItemPreviewResponse("B", "messages", "2m", 1, "Review") { PriorityScore = 75 },
            new InboxItemPreviewResponse("C", "messages", "3m", 1, "Review") { PriorityScore = 74 },
            new InboxItemPreviewResponse("D", "messages", "4m", 1, "Review") { PriorityScore = null }
        };

        var count = PriorityPresentation.GetHighPriorityCount(items);

        Assert.Equal(2, count);
    }

    [Theory]
    [InlineData(null, null, 0)]
    [InlineData(null, "Low", 25)]
    [InlineData(null, "Medium", 50)]
    [InlineData(null, "High", 75)]
    [InlineData(null, "Critical", 100)]
    [InlineData(80, null, 80)]
    [InlineData(80, "Low", 80)]   // PriorityScore wins over PriorityHint
    public void ResolveEffectivePriorityScore_UsesPriorityScoreFirst_ThenPriorityHint(
        int? priorityScore, string? priorityHint, int expected)
    {
        var item = new InboxItemPreviewResponse("X", "messages", "1m", 1, "Review")
        {
            PriorityScore = priorityScore,
            PriorityHint = priorityHint
        };

        var result = PriorityPresentation.ResolveEffectivePriorityScore(item);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetHighPriorityCount_FallsBackToPriorityHint_WhenPriorityScoreIsNull()
    {
        var items = new[]
        {
            new InboxItemPreviewResponse("A", "messages", "1m", 1, "Review") { PriorityScore = null, PriorityHint = "High" },
            new InboxItemPreviewResponse("B", "messages", "2m", 1, "Review") { PriorityScore = null, PriorityHint = "Critical" },
            new InboxItemPreviewResponse("C", "messages", "3m", 1, "Review") { PriorityScore = null, PriorityHint = "Low" },
            new InboxItemPreviewResponse("D", "messages", "4m", 1, "Review") { PriorityScore = 80 },
        };

        var count = PriorityPresentation.GetHighPriorityCount(items);

        Assert.Equal(3, count); // A(High=75) + B(Critical=100) + D(80)
    }

    [Theory]
    [InlineData("high", true)]
    [InlineData("High", true)]    // Capitalized — como viene del enum .ToString()
    [InlineData("HIGH", true)]
    [InlineData("Critical", true)]
    [InlineData("critical", true)]
    [InlineData("Medium", false)]
    [InlineData("Low", false)]
    public void GetHighPriorityPrCount_MatchesPriorityCaseInsensitive(string? priority, bool expected)
    {
        var items = new[]
        {
            new PrPreviewItemResponse("PR Title", "PR #1 Title", "main", "passing", 1, 1, 0, "alice", DateTimeOffset.UtcNow, "1m ago", "https://dev.azure.com", false, priority)
        };

        var count = PriorityPresentation.GetHighPriorityPrCount(items);

        Assert.Equal(expected ? 1 : 0, count);
    }
}
