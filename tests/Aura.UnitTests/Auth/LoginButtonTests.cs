using Aura.UI.Components.Auth;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;

namespace Aura.UnitTests.Auth;

public class LoginButtonTests : TestContext
{
    private void SetupServices(IAuthPopupService? popupServiceOverride = null)
    {
        var popupService = popupServiceOverride ?? Substitute.For<IAuthPopupService>();
        popupService.InitializeAsync(Arg.Any<IJSRuntime>()).Returns(ValueTask.CompletedTask);
        Services.AddSingleton(popupService);
        Services.AddSingleton(Substitute.For<IJSRuntime>());
    }

    [Fact]
    public void Button_RendersWithConfiguredText()
    {
        // Arrange
        SetupServices();

        // Act
        var cut = RenderComponent<LoginButton>(p => p
            .Add(x => x.ButtonText, "Login / Access Aura")
            .Add(x => x.CssClass, "my-cta"));

        // Assert
        var button = cut.Find("button");
        Assert.Contains("Login / Access Aura", button.TextContent);
        Assert.Contains("my-cta", button.GetAttribute("class"));
    }

    [Fact]
    public void Button_RendersDefaultText_WhenNoParamProvided()
    {
        // Arrange
        SetupServices();

        // Act
        var cut = RenderComponent<LoginButton>();

        // Assert
        var button = cut.Find("button");
        Assert.Contains("Login", button.TextContent);
    }

    [Fact]
    public async Task PopupSuccess_NavigatesToDashboard()
    {
        // Arrange
        var popupService = Substitute.For<IAuthPopupService>();
        popupService.InitializeAsync(Arg.Any<IJSRuntime>()).Returns(ValueTask.CompletedTask);
        popupService.OpenMicrosoftLoginPopupAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        popupService.WaitForPopupResultAsync(Arg.Any<CancellationToken>())
            .Returns(new AuthResult("test-token", true, null));

        var navManager = new TrackingNavigationManager("http://localhost/");
        Services.AddSingleton<IAuthPopupService>(popupService);
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        Services.AddSingleton<NavigationManager>(navManager);

        var cut = RenderComponent<LoginButton>(p => p
            .Add(x => x.ButtonText, "Login"));

        // Act — click the button
        await cut.Find("button").ClickAsync(new());

        // Assert — navigates to /dashboard
        Assert.True(navManager.NavigateToCalled);
        Assert.Equal("/dashboard", navManager.LastUri);
    }

    [Fact]
    public async Task InvalidOperationException_ShowsFallbackLink()
    {
        // Arrange
        var popupService = Substitute.For<IAuthPopupService>();
        popupService.InitializeAsync(Arg.Any<IJSRuntime>()).Returns(ValueTask.CompletedTask);
        popupService.OpenMicrosoftLoginPopupAsync(Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("Popup blocked")));

        SetupServices(popupService);

        var cut = RenderComponent<LoginButton>(p => p
            .Add(x => x.ButtonText, "Login"));

        // Act
        await cut.Find("button").ClickAsync(new());

        // Assert — fallback link is shown
        Assert.NotNull(cut.Find("[data-testid='login-button-blocked']"));
        Assert.NotNull(cut.Find("[data-testid='login-button-fallback-link']"));
    }

    [Fact]
    public async Task JSException_ShowsFallbackLink()
    {
        // Arrange
        var popupService = Substitute.For<IAuthPopupService>();
        popupService.InitializeAsync(Arg.Any<IJSRuntime>()).Returns(ValueTask.CompletedTask);
        popupService.OpenMicrosoftLoginPopupAsync(Arg.Any<string>())
            .Returns(Task.FromException(new JSException("JS interop failed")));

        SetupServices(popupService);

        var cut = RenderComponent<LoginButton>(p => p
            .Add(x => x.ButtonText, "Login"));

        // Act
        await cut.Find("button").ClickAsync(new());

        // Assert — fallback link is shown
        Assert.NotNull(cut.Find("[data-testid='login-button-blocked']"));
    }

    [Fact]
    public async Task LoadingState_DisablesButton()
    {
        // Arrange — popup service that delays to keep loading state observable
        var popupService = Substitute.For<IAuthPopupService>();
        popupService.InitializeAsync(Arg.Any<IJSRuntime>()).Returns(ValueTask.CompletedTask);

        var tcs = new TaskCompletionSource();
        popupService.OpenMicrosoftLoginPopupAsync(Arg.Any<string>())
            .Returns(tcs.Task);

        SetupServices(popupService);

        var cut = RenderComponent<LoginButton>(p => p
            .Add(x => x.ButtonText, "Login"));

        // Act — click the button (don't await completion)
        var clickTask = cut.Find("button").ClickAsync(new());

        // Assert — button should be disabled during loading
        cut.WaitForAssertion(() =>
        {
            var button = cut.Find("button");
            Assert.NotNull(button.GetAttribute("disabled"));
        }, TimeSpan.FromSeconds(2));

        // Cleanup — release the pending task
        tcs.SetResult();
    }

    [Fact]
    public void DataTestId_IsRenderedOnButton()
    {
        // Arrange
        SetupServices();

        // Act
        var cut = RenderComponent<LoginButton>(p => p
            .Add(x => x.DataTestId, "hero-login-btn"));

        // Assert
        var button = cut.Find("button");
        Assert.Equal("hero-login-btn", button.GetAttribute("data-testid"));
    }

    /// <summary>
    /// NavigationManager that tracks NavigateTo calls.
    /// </summary>
    private sealed class TrackingNavigationManager : NavigationManager
    {
        public bool NavigateToCalled { get; private set; }
        public string? LastUri { get; private set; }

        public TrackingNavigationManager(string uri)
        {
            Initialize("http://localhost/", uri);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigateToCalled = true;
            LastUri = uri;
        }
    }
}
