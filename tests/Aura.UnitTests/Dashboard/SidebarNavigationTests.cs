using Aura.UI.Components.Layout;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

/// <summary>
/// Tests that <see cref="Sidebar"/> renders navigation links correctly.
/// </summary>
public class SidebarNavigationTests : TestContext
{
    public SidebarNavigationTests()
    {
        Services.AddSingleton(new AppVersionService());
        Services.AddSingleton(new DemoUiState());
        // Don't register NavigationManager — bUnit provides its own
        var jsRuntime = Substitute.For<IJSRuntime>();
        Services.AddSingleton(jsRuntime);
        Services.AddSingleton(Substitute.For<IHttpClientFactory>());
        Services.AddSingleton<IDashboardEventBus>(new DashboardEventBus());
        Services.AddSingleton(Substitute.For<IFocusStateApiClient>());
        Services.AddSingleton(Substitute.For<IDashboardEventBus>());
        Services.AddLogging();
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
