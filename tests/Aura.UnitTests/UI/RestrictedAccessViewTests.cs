using Aura.UI.Components.Auth;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.UI;

public class RestrictedAccessViewTests : TestContext
{
    [Fact]
    public void UnauthenticatedUser_ShouldSeeRestrictedAccessView()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseEntraId"] = "false"
            })
            .Build();

        Services.AddSingleton<IConfiguration>(config);

        // Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='restricted-access-view']"));
    }

    [Fact]
    public void UseEntraIdTrue_ShouldShowOnlyMicrosoftButton_HideDevButton()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseEntraId"] = "true"
            })
            .Build();

        Services.AddSingleton<IConfiguration>(config);

        // Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='login-microsoft-btn']"));
        var devButtons = cut.FindAll("[data-testid='login-dev-btn']");
        Assert.Empty(devButtons);
    }

    [Fact]
    public void UseEntraIdFalse_ShouldShowBothButtons()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseEntraId"] = "false"
            })
            .Build();

        Services.AddSingleton<IConfiguration>(config);

        // Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='login-microsoft-btn']"));
        Assert.NotNull(cut.Find("[data-testid='login-dev-btn']"));
    }

    [Fact]
    public void DevLogin_Click_RedirectsToDevEndpoint()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseEntraId"] = "false"
            })
            .Build();

        Services.AddSingleton<IConfiguration>(config);

        // Act
        var cut = RenderComponent<RestrictedAccessView>();
        cut.Find("[data-testid='login-dev-btn']").Click();

        // Assert
        Assert.EndsWith("/login/dev", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void MicrosoftLogin_Click_RedirectsToChallenge()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseEntraId"] = "true"
            })
            .Build();

        Services.AddSingleton<IConfiguration>(config);

        // Act
        var cut = RenderComponent<RestrictedAccessView>();
        cut.Find("[data-testid='login-microsoft-btn']").Click();

        // Assert — direct redirect to the OIDC challenge endpoint (no popup)
        Assert.EndsWith("/login/challenge", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }
}
