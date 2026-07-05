using Aura.UI.Components.Layout;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

public class FocusStateBadgeTests : TestContext
{
    [Fact]
    public void RendersCurrentStateBadgeText()
    {
        var api = Substitute.For<IFocusStateApiClient>();
        api.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new FocusStateResponse("WindowOfOpportunity", false, "user-123")));
        Services.AddSingleton(api);

        var cut = RenderComponent<FocusStateBadge>();

        cut.WaitForElement("[data-testid='focus-state-badge']");
        Assert.Contains("Window of Opportunity", cut.Markup);
    }

    [Fact]
    public void ShowsClearOverrideOption_WhenOverrideIsActive()
    {
        var api = Substitute.For<IFocusStateApiClient>();
        api.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new FocusStateResponse("DeepWork", true, "user-123")));
        Services.AddSingleton(api);

        var cut = RenderComponent<FocusStateBadge>();
        cut.WaitForElement("[data-testid='focus-state-badge']").Click();

        Assert.NotNull(cut.Find("[data-testid='focus-state-clear-override']"));
    }

    [Fact]
    public void SelectingState_CallsSetOverrideApi()
    {
        var api = Substitute.For<IFocusStateApiClient>();
        api.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new FocusStateResponse("Recovery", false, "user-123")));
        Services.AddSingleton(api);

        var cut = RenderComponent<FocusStateBadge>();
        cut.WaitForElement("[data-testid='focus-state-badge']").Click();
        cut.Find("[data-testid='focus-state-option-DeepWork']").Click();

        api.Received(1).SetOverrideAsync("DeepWork", Arg.Any<CancellationToken>());
    }
}
