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
}
