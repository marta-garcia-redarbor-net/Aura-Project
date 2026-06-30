using Aura.UI.Components.Dashboard;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.UI;

public class RankedSummaryListTests : TestContext
{
    [Fact]
    public void RankedSummaryList_Empty_ShowsEmptyState()
    {
        // Arrange & Act
        var cut = RenderComponent<RankedSummaryList>();

        // Assert: empty state message
        Assert.Contains("No pending items", cut.Markup);
        Assert.Contains("data-testid=\"ranked-summary-empty\"", cut.Markup);
    }

    [Fact]
    public void RankedSummaryList_Null_ShowsEmptyState()
    {
        // Arrange & Act: Items is null by default
        var cut = RenderComponent<RankedSummaryList>(parameters => parameters
            .Add(p => p.Items, null));

        // Assert
        Assert.Contains("No pending items", cut.Markup);
    }

    [Fact]
    public void RankedSummaryList_WithItems_RendersList()
    {
        // Arrange
        var items = new List<RankedSummaryList.SummaryItem>
        {
            new() { Source = "Outlook", Title = "New email", Snippet = "Meeting request", Score = 95 },
            new() { Source = "Teams", Title = "New message", Snippet = "Urgent question", Score = 80 }
        };

        // Act
        var cut = RenderComponent<RankedSummaryList>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert: items are rendered
        Assert.Contains("New email", cut.Markup);
        Assert.Contains("Meeting request", cut.Markup);
        Assert.Contains("New message", cut.Markup);
        Assert.Contains("Urgent question", cut.Markup);
        Assert.Contains("data-testid=\"ranked-summary-list\"", cut.Markup);
    }

    [Fact]
    public void RankedSummaryList_ShowsScoreForEachItem()
    {
        // Arrange
        var items = new List<RankedSummaryList.SummaryItem>
        {
            new() { Source = "Outlook", Title = "Item 1", Snippet = "Snippet 1", Score = 42 }
        };

        // Act
        var cut = RenderComponent<RankedSummaryList>(parameters => parameters
            .Add(p => p.Items, items));

        // Assert: score is visible
        Assert.Contains("42", cut.Markup);
        Assert.Contains("data-testid=\"summary-score\"", cut.Markup);
    }
}
