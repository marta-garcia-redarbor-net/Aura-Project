using Aura.UI.Components.Dashboard;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

public class InboxPreviewPanelPriorityTests : TestContext
{
    [Fact]
    public void RendersImportanceBadge_ForTopPriorityItemsInGroup()
    {
        var previewClient = Substitute.For<IDashboardPreviewApiClient>();
        previewClient.GetPreviewAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DashboardPreviewResponse(
                [
                    new InboxSourceGroupResponse("messages",
                    [
                        new InboxItemPreviewResponse("A", "messages", "1m ago", 1, "Review") { PriorityScore = 95, PriorityHint = "Critical" },
                        new InboxItemPreviewResponse("B", "messages", "2m ago", 1, "Review") { PriorityScore = 90, PriorityHint = "High" },
                        new InboxItemPreviewResponse("C", "messages", "3m ago", 1, "Review") { PriorityScore = 85, PriorityHint = "High" },
                        new InboxItemPreviewResponse("D", "messages", "4m ago", 1, "Review") { PriorityScore = 85, PriorityHint = "High" },
                        new InboxItemPreviewResponse("E", "messages", "5m ago", 1, "Review") { PriorityScore = 70, PriorityHint = "Medium" }
                    ])
                ],
                [])));
        Services.AddSingleton(previewClient);

        var cut = RenderComponent<InboxPreviewPanel>();
        cut.WaitForElement("[data-testid='inbox-preview-populated']");

        var badges = cut.FindAll("[data-testid='inbox-preview-importance-badge']");
        Assert.Equal(4, badges.Count);
    }
}
