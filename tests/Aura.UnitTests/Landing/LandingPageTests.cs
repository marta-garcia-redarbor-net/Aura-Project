using System.Security.Claims;
using Aura.UI.Components.Auth;
using Aura.UI.Components.Layout;
using Aura.UI.Components.Pages;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;

namespace Aura.UnitTests.Landing;

public class LandingPageTests : TestContext
{
    private static AuthenticationState CreateAnonymousState()
    {
        var identity = new ClaimsIdentity(); // no authentication type = anonymous
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private static AuthenticationState CreateAuthenticatedState()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "Test User")], "TestAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private void SetupServices()
    {
        Services.AddSingleton(Substitute.For<IAuthPopupService>());
        Services.AddSingleton(Substitute.For<IJSRuntime>());
    }

    [Fact]
    public void AnonymousUser_SeesLandingPageSections()
    {
        // Arrange
        SetupServices();
        Task<AuthenticationState> authState = Task.FromResult(CreateAnonymousState());

        // Act
        var cut = RenderComponent<LandingPage>(p => p.AddCascadingValue(authState));

        // Assert — all major sections render for anonymous users
        Assert.NotNull(cut.Find("[data-testid='landing-header']"));
        Assert.NotNull(cut.Find("[data-testid='landing-hero']"));
        Assert.NotNull(cut.Find("[data-testid='landing-problems']"));
        Assert.NotNull(cut.Find("[data-testid='landing-features']"));
        Assert.NotNull(cut.Find("[data-testid='landing-cta']"));
        Assert.NotNull(cut.Find("[data-testid='landing-footer']"));
    }

    [Fact]
    public void AnonymousUser_SeesBothHeroCTAs()
    {
        // Arrange
        SetupServices();
        Task<AuthenticationState> authState = Task.FromResult(CreateAnonymousState());

        // Act
        var cut = RenderComponent<LandingPage>(p => p.AddCascadingValue(authState));

        // Assert — hero has both login and demo buttons
        Assert.NotNull(cut.Find("[data-testid='hero-login-btn']"));
        Assert.NotNull(cut.Find("[data-testid='hero-demo-btn']"));
    }

    [Fact]
    public void AnonymousUser_SeesHeaderLoginButton()
    {
        // Arrange
        SetupServices();
        Task<AuthenticationState> authState = Task.FromResult(CreateAnonymousState());

        // Act
        var cut = RenderComponent<LandingPage>(p => p.AddCascadingValue(authState));

        // Assert — header has login button
        Assert.NotNull(cut.Find("[data-testid='landing-login-btn']"));
    }

    [Fact]
    public void AuthenticatedUser_RedirectsToDashboard()
    {
        // Arrange
        SetupServices();
        var navManager = new TrackingNavigationManager("http://localhost/");
        Services.AddSingleton<NavigationManager>(navManager);
        Task<AuthenticationState> authState = Task.FromResult(CreateAuthenticatedState());

        // Act
        RenderComponent<LandingPage>(p => p.AddCascadingValue(authState));

        // Assert — navigation to /dashboard was triggered
        Assert.True(navManager.NavigateToCalled, "NavigateTo should be called for authenticated users");
        Assert.Equal("/dashboard", navManager.LastUri);
    }

    [Fact]
    public void DemoButton_HasCorrectTestId()
    {
        // Arrange
        SetupServices();
        Task<AuthenticationState> authState = Task.FromResult(CreateAnonymousState());

        // Act
        var cut = RenderComponent<LandingPage>(p => p.AddCascadingValue(authState));

        // Assert — demo button has the correct data-testid
        var demoBtn = cut.Find("[data-testid='hero-demo-btn']");
        Assert.Contains("Explore Demo Mode", demoBtn.TextContent);
    }

    /// <summary>
    /// NavigationManager that tracks NavigateTo calls for assertion.
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
