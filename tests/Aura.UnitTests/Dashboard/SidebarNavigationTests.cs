using Aura.UI.Components.Layout;
using Bunit;

namespace Aura.UnitTests.Dashboard;

/// <summary>
/// Tests that <see cref="Sidebar"/> renders the Health navigation link
/// as an anchor element with href="/health".
/// </summary>
public class SidebarNavigationTests : TestContext
{
    [Fact]
    public void Sidebar_HealthLink_IsAnchorWithHealthHref()
    {
        // Act
        var cut = RenderComponent<Sidebar>();

        // Assert: find the Health nav item — it should be an <a> with href="/health"
        var healthLink = cut.Find("a[href='/health']");
        Assert.NotNull(healthLink);
        Assert.Contains("Health", healthLink.TextContent);
    }

    [Fact]
    public void Sidebar_HealthLink_HasCorrectNavClass()
    {
        // Act
        var cut = RenderComponent<Sidebar>();

        // Assert
        var healthLink = cut.Find("a[href='/health']");
        Assert.Contains("dashboard-sidebar__nav-item", healthLink.GetAttribute("class"));
    }

    [Fact]
    public void Sidebar_InterruptionLogLink_IsAnchorWithDecisionsHref()
    {
        var cut = RenderComponent<Sidebar>();

        var decisionsLink = cut.Find("a[href='/triage/decisions']");
        Assert.NotNull(decisionsLink);
        Assert.Contains("Interruption Log", decisionsLink.TextContent);
    }
}
