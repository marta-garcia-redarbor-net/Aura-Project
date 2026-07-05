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
}
