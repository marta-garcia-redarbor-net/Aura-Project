using System.Net;
using System.Text;
using Aura.UI.Components.Auth;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;

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
        Services.AddSingleton(Substitute.For<IAuthPopupService>());
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        Services.AddSingleton(Substitute.For<HttpClient>());

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
        Services.AddSingleton(Substitute.For<IAuthPopupService>());
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        Services.AddSingleton(Substitute.For<HttpClient>());

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
        Services.AddSingleton(Substitute.For<IAuthPopupService>());
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        Services.AddSingleton(Substitute.For<HttpClient>());

        // Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='login-microsoft-btn']"));
        Assert.NotNull(cut.Find("[data-testid='login-dev-btn']"));
    }

    [Fact]
    public void DevLogin_ClickMockButton_NavigatesToDevLoginEndpoint()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseEntraId"] = "false"
            })
            .Build();

        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton(Substitute.For<IAuthPopupService>());
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        Services.AddSingleton(Substitute.For<HttpClient>());

        // Act
        var cut = RenderComponent<RestrictedAccessView>();
        cut.Find("[data-testid='login-dev-btn']").Click();

        // Assert — component delegates dev login to the server endpoint route
        Assert.EndsWith("/login/dev", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void MicrosoftLogin_PopupBlocked_ShowsFallbackMessage()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseEntraId"] = "true"
            })
            .Build();

        var mockPopupService = Substitute.For<IAuthPopupService>();
        var blockedTask = Task.FromException(new InvalidOperationException("Popup blocked"));
        mockPopupService
            .OpenMicrosoftLoginPopupAsync(Arg.Any<string>())
            .Returns(blockedTask);

        Services.AddSingleton<IConfiguration>(config);
        Services.AddSingleton<IAuthPopupService>(mockPopupService);
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        Services.AddSingleton(Substitute.For<HttpClient>());

        // Act
        var cut = RenderComponent<RestrictedAccessView>();
        cut.Find("[data-testid='login-microsoft-btn']").Click();

        // Assert — blocked popup message and toast are shown
        Assert.NotNull(cut.Find("[data-testid='popup-blocked-message']"));
        Assert.Contains("Pop-ups blocked", cut.Find("[data-testid='popup-blocked-message']").TextContent);
        Assert.NotNull(cut.Find("[data-testid='fallback-redirect-link']"));
        Assert.NotNull(cut.Find("[data-testid='blocked-toast']"));
        Assert.Contains("Pop-up blocked", cut.Find("[data-testid='blocked-toast']").TextContent);
    }

    // ──────────────────────────────────────────────────────────────────────
    // login-popup REDESIGN tests (tasks 2.1 + 2.2)
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void UnauthenticatedUser_Redesign_RendersRestrictedViewWithoutRedirect()
    {
        // Arrange — UseEntraId=true; no AzureAd keys (none needed after redesign)
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseEntraId"] = "true"
            })
            .Build();

        IAuthPopupService mockPopupService = Substitute.For<IAuthPopupService>();
        mockPopupService
            .OpenMicrosoftLoginPopupAsync(Arg.Any<string>())
            .Returns(Task.CompletedTask);

        Services.AddSingleton(config);
        Services.AddSingleton(mockPopupService);
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        Services.AddSingleton(Substitute.For<HttpClient>());

        // Act
        IRenderedComponent<RestrictedAccessView> cut = RenderComponent<RestrictedAccessView>();

        // Assert — restricted view must render immediately, no redirect
        Assert.NotNull(cut.Find("[data-testid='restricted-access-view']"));
    }

    [Fact]
    public async Task MicrosoftLoginButton_Redesign_CallsPopupServiceWithLoginChallengeUrl()
    {
        // Arrange — UseEntraId=true; after redesign, popup URL must be "/login/challenge"
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseEntraId"] = "true"
            })
            .Build();

        IAuthPopupService mockPopupService = Substitute.For<IAuthPopupService>();
        mockPopupService
            .OpenMicrosoftLoginPopupAsync(Arg.Any<string>())
            .Returns(Task.CompletedTask);
        // WaitForPopupResultAsync returns null by default — no crash expected
        mockPopupService
            .WaitForPopupResultAsync(Arg.Any<CancellationToken>())
            .Returns((AuthResult?)null);

        Services.AddSingleton(config);
        Services.AddSingleton(mockPopupService);
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        Services.AddSingleton(Substitute.For<IHttpContextAccessor>());
        Services.AddSingleton(Substitute.For<HttpClient>());

        IRenderedComponent<RestrictedAccessView> cut = RenderComponent<RestrictedAccessView>();

        // Act — click the Microsoft login button
        cut.Find("[data-testid='login-microsoft-btn']").Click();

        // Wait briefly for async handler to complete
        cut.WaitForAssertion(() =>
            mockPopupService.Received(1)
                .OpenMicrosoftLoginPopupAsync("/login/challenge?popup=true"),
            TimeSpan.FromSeconds(2));

        // Assert — also verify directly via NSubstitute
        await mockPopupService.Received(1)
            .OpenMicrosoftLoginPopupAsync("/login/challenge?popup=true");
    }

}
