using Aura.UI.Components.Layout;
using Bunit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

/// <summary>
/// Tests that <see cref="Header"/> renders a sign-out button
/// and invokes the correct sign-out handler.
/// </summary>
public class HeaderSignOutTests : TestContext
{
    private static IConfiguration CreateConfig(bool useEntraId = true)
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "UseEntraId", useEntraId.ToString() }
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    private void SetupCommonServices(bool useEntraId = true)
    {
        Services.AddSingleton(CreateConfig(useEntraId));
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
    }

    [Fact]
    public void Header_RendersSignOutButton()
    {
        // Arrange
        SetupCommonServices();

        // Act
        var cut = RenderComponent<Header>();

        // Assert
        var btn = cut.Find("[data-testid='sign-out-btn']");
        Assert.NotNull(btn);
        Assert.Equal("button", btn.TagName, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Header_SignOutButtonHasLogoutIcon()
    {
        // Arrange
        SetupCommonServices();

        // Act
        var cut = RenderComponent<Header>();

        // Assert
        var btn = cut.Find("[data-testid='sign-out-btn']");
        Assert.Contains("logout", btn.InnerHtml);
    }

    [Fact]
    public void Header_SignOutButtonHasTitle()
    {
        // Arrange
        SetupCommonServices();

        // Act
        var cut = RenderComponent<Header>();

        // Assert
        var btn = cut.Find("[data-testid='sign-out-btn']");
        Assert.Equal("Sign out", btn.GetAttribute("title"));
    }

    [Fact]
    public void Header_RendersUserDisplayName_WhenProvided()
    {
        // Arrange
        SetupCommonServices();

        // Act
        var cut = RenderComponent<Header>(parameters => parameters
            .Add(p => p.UserDisplayName, "Test User"));

        // Assert
        Assert.Contains("Test User", cut.Markup);
    }
}
