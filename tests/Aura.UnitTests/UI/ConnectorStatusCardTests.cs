using Aura.UI.Components.Dashboard;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.UI;

public class ConnectorStatusCardTests : TestContext
{
    [Fact]
    public void ConnectorStatusCard_Renders_NameAndItemCount()
    {
        // Arrange & Act
        var cut = RenderComponent<ConnectorStatusCard>(parameters => parameters
            .Add(p => p.Name, "Outlook")
            .Add(p => p.Status, "Healthy")
            .Add(p => p.ItemCount, 5)
            .Add(p => p.LastSyncTime, "2 min ago"));

        // Assert: name and item count are rendered
        Assert.Contains("Outlook", cut.Markup);
        Assert.Contains("5", cut.Markup);
        Assert.Contains("2 min ago", cut.Markup);
    }

    [Fact]
    public void ConnectorStatusCard_Renders_TeamsWithItemCount()
    {
        // Arrange & Act
        var cut = RenderComponent<ConnectorStatusCard>(parameters => parameters
            .Add(p => p.Name, "Teams")
            .Add(p => p.Status, "Healthy")
            .Add(p => p.ItemCount, 10)
            .Add(p => p.LastSyncTime, "1 min ago"));

        // Assert
        Assert.Contains("Teams", cut.Markup);
        Assert.Contains("10", cut.Markup);
    }

    [Fact]
    public void ConnectorStatusCard_Disabled_ShowsNeverSyncTime()
    {
        // Arrange & Act
        var cut = RenderComponent<ConnectorStatusCard>(parameters => parameters
            .Add(p => p.Name, "Calendar")
            .Add(p => p.Status, "Disabled")
            .Add(p => p.ItemCount, 0)
            .Add(p => p.LastSyncTime, "Never"));

        // Assert: name, zero items, and "Never" sync time rendered
        Assert.Contains("Calendar", cut.Markup);
        Assert.Contains("Never", cut.Markup);
    }

    [Fact]
    public void ConnectorStatusCard_Warning_RendersWithWarningStatus()
    {
        // Arrange: create with Warning status
        var cut = RenderComponent<ConnectorStatusCard>(parameters => parameters
            .Add(p => p.Name, "Outlook")
            .Add(p => p.Status, "Warning")
            .Add(p => p.ItemCount, 2)
            .Add(p => p.LastSyncTime, "5 min ago"));

        // Assert: component renders (status affects CSS class, not text)
        Assert.Contains("Outlook", cut.Markup);
        Assert.Contains("2", cut.Markup);
        Assert.Contains("5 min ago", cut.Markup);
    }
}
