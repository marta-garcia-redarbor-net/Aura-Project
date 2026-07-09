using Aura.UI.Components.Auth;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.UI;

/// <summary>
/// Tests for the simplified RestrictedAccessView.
/// After the landing page rework, this view is a simple overlay with a link to the landing page.
/// The full login experience (Microsoft sign-in, demo mode) is now on the landing page.
/// </summary>
public class RestrictedAccessViewTests : TestContext
{
    [Fact]
    public void UnauthenticatedUser_ShouldSeeRestrictedAccessView()
    {
        // Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='restricted-access-view']"));
    }

    [Fact]
    public void RestrictedView_ShowsLinkToLandingPage()
    {
        // Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert — has a link/button to the landing page
        Assert.NotNull(cut.Find("[data-testid='restricted-go-login-btn']"));
    }

    [Fact]
    public void RestrictedView_LinkPointsToRoot()
    {
        // Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert — the go-to-login button links to /
        var link = cut.Find("[data-testid='restricted-go-login-btn']");
        Assert.Equal("/", link.GetAttribute("href"));
    }

    [Fact]
    public void RestrictedView_ShowsAuthRequiredMessage()
    {
        // Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert — shows a message about authentication being required
        Assert.Contains("Authentication required", cut.Markup);
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

    [Fact]
    public void RestrictedView_RendersBlurredDashboardShell()
    {
        // Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert — blurred sidebar and header are present
        Assert.NotNull(cut.Find("[data-testid='blurred-sidebar']"));
        Assert.NotNull(cut.Find("[data-testid='blurred-header']"));
    }

    [Fact]
    public void RestrictedView_RendersLoginCard()
    {
        // Act
        var cut = RenderComponent<RestrictedAccessView>();

        // Assert — login card overlay is present
        Assert.NotNull(cut.Find("[data-testid='login-card']"));
    }
}
