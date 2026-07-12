using Aura.UI.Components.Layout;
using Aura.UI.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.Dashboard;

/// <summary>
/// Tests that <see cref="Sidebar"/> renders navigation links correctly.
/// </summary>
public class SidebarNavigationTests : TestContext
{
    public SidebarNavigationTests()
    {
        Services.AddSingleton(new AppVersionService());
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
