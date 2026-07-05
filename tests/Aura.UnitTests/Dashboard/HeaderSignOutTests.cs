using Aura.UI.Components.Layout;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
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

        var focusStateApi = Substitute.For<IFocusStateApiClient>();
        focusStateApi.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new FocusStateResponse("WindowOfOpportunity", false, "user-123")));
        Services.AddSingleton(focusStateApi);
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

    [Fact]
    public void Header_RendersTopPriorityQueueLink_InHeaderNavPosition()
    {
        SetupCommonServices();

        var cut = RenderComponent<Header>();
        cut.WaitForElement("[data-testid='header-top-priority-counter']");

        var nav = cut.Find(".dashboard-header__nav");
        Assert.Contains("Top priority queue", nav.TextContent);
    }

    [Fact]
    public void Header_ClickingTopPriorityQueueLink_NavigatesToTopPriorityRoute()
    {
        SetupCommonServices();
        var navManager = Services.GetRequiredService<NavigationManager>();

        var cut = RenderComponent<Header>();
        cut.WaitForElement("[data-testid='header-top-priority-counter']").Click();

        Assert.EndsWith("/top-priority", navManager.Uri, StringComparison.Ordinal);
    }

}
