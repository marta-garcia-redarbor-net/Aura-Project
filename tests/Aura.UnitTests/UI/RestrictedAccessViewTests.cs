using Aura.UI.Components.Auth;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;

namespace Aura.UnitTests.UI;

/// <summary>
/// Tests for the simplified RestrictedAccessView.
/// After the landing page rework, this view redirects to the landing page with a toast notification.
/// It no longer shows a full-screen overlay.
/// </summary>
public class RestrictedAccessViewTests : TestContext
{
    private readonly TestNavigationManager _navManager;

    public RestrictedAccessViewTests()
    {
        _navManager = new TestNavigationManager();
        Services.AddSingleton<NavigationManager>(_navManager);
        Services.AddSingleton(Substitute.For<IJSRuntime>());
    }

    [Fact]
    public void UnauthenticatedUser_ShouldRedirectToLogout()
    {
        // Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert — component triggers navigation to /logout
        cut.WaitForAssertion(() =>
        {
            Assert.True(_navManager.NavigateToCalled, "NavigateTo should be called");
            Assert.Equal("/logout", _navManager.LastUri);
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void RestrictedView_DoesNotRenderAnyVisibleContent()
    {
        // Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert — component no longer renders visible UI (just redirects)
        Assert.Empty(cut.Markup.Trim());
    }

    [Fact]
    public void RestrictedView_DoesNotShowMicrosoftLoginButton()
    {
        // Arrange & Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert — Microsoft login popup flow moved to landing page
        var microsoftButtons = cut.FindAll("[data-testid='login-microsoft-btn']");
        Assert.Empty(microsoftButtons);
    }

    [Fact]
    public void RestrictedView_DoesNotShowDevLoginButton()
    {
        // Arrange & Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert — dev login button removed (now handled by landing page)
        var devButtons = cut.FindAll("[data-testid='login-dev-btn']");
        Assert.Empty(devButtons);
    }

    /// <summary>
    /// NavigationManager that tracks NavigateTo calls for assertion.
    /// </summary>
    private sealed class TestNavigationManager : NavigationManager
    {
        public bool NavigateToCalled { get; private set; }
        public string? LastUri { get; private set; }

        public TestNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigateToCalled = true;
            LastUri = uri;
        }
    }
}
