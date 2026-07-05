using Aura.UI.Components.Layout;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

public class HeaderFocusStateBadgeTests : TestContext
{
    private static IConfiguration CreateConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseEntraId"] = "false"
            })
            .Build();
    }

    [Fact]
    public void Header_RendersFocusStateBadge_WithCurrentState()
    {
        var api = Substitute.For<IFocusStateApiClient>();
        api.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new FocusStateResponse("WindowOfOpportunity", false, "user-123")));

        Services.AddSingleton(CreateConfig());
        Services.AddSingleton(api);

        var cut = RenderComponent<Header>();

        cut.WaitForElement("[data-testid='focus-state-badge']");
        Assert.Contains("Window of Opportunity", cut.Markup);
    }

    [Fact]
    public void Header_FocusStateDropdown_SelectingOverride_CallsApiAndUpdatesBadge()
    {
        var api = Substitute.For<IFocusStateApiClient>();
        api.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new FocusStateResponse("Recovery", false, "user-123")));

        Services.AddSingleton(CreateConfig());
        Services.AddSingleton(api);

        var cut = RenderComponent<Header>();

        cut.WaitForElement("[data-testid='focus-state-badge']").Click();
        cut.Find("[data-testid='focus-state-option-DeepWork']").Click();

        api.Received(1).SetOverrideAsync("DeepWork", Arg.Any<CancellationToken>());
        cut.WaitForAssertion(() => Assert.Contains("Deep Work", cut.Markup));
    }
}
