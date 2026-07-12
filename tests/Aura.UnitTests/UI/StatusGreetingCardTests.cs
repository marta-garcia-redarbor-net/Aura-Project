using Aura.UI.Components.Dashboard;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.UI;

public class StatusGreetingCardTests : TestContext
{
    private readonly ISystemStatusApiClient _apiClient;
    private readonly IDashboardEventBus _eventBus;

    public StatusGreetingCardTests()
    {
        _apiClient = Substitute.For<ISystemStatusApiClient>();
        _eventBus = Substitute.For<IDashboardEventBus>();

        Services.AddScoped(_ => _apiClient);
        Services.AddScoped(_ => _eventBus);
        Services.AddAuthorizationCore();
        Services.AddCascadingAuthenticationState();
    }

    private void SetupAllOk()
    {
        _apiClient.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new SystemStatusResponse(
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "API ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "Qdrant ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "MockAuth ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "DB ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "LLM ok")));
    }

    private void SetupMixedStates()
    {
        _apiClient.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new SystemStatusResponse(
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "API ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "Qdrant ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "MockAuth ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Error, "DB error"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "LLM ok")));
    }

    private static AuthenticationState CreateAuthState(string displayName = "Test User")
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
            new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, displayName)
            },
            "TestAuth");
        return new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(identity));
    }

    [Fact]
    public void StatusGreetingCard_WhenAllHealthy_ShowsAllGreenBadges()
    {
        SetupAllOk();
        var authState = Task.FromResult(CreateAuthState());

        var cut = RenderComponent<StatusGreetingCard>(parameters => parameters
            .AddCascadingValue(authState));

        // Card should render
        Assert.NotNull(cut.Find("[data-testid='status-greeting-card']"));

        // All five badges present with expected labels
        var apiBadge = cut.Find("[data-testid='status-badge-api']");
        var dbBadge = cut.Find("[data-testid='status-badge-db']");
        var authBadge = cut.Find("[data-testid='status-badge-auth']");
        var qdrantBadge = cut.Find("[data-testid='status-badge-qdrant']");
        var llmBadge = cut.Find("[data-testid='status-badge-llm']");

        // All should be healthy (green)
        Assert.Contains("status-badge--healthy", apiBadge.ClassName);
        Assert.Contains("status-badge--healthy", dbBadge.ClassName);
        Assert.Contains("status-badge--healthy", authBadge.ClassName);
        Assert.Contains("status-badge--healthy", qdrantBadge.ClassName);
        Assert.Contains("status-badge--healthy", llmBadge.ClassName);

        // aria-labels should describe the healthy state
        Assert.Equal("API: Healthy", apiBadge.GetAttribute("aria-label"));
        Assert.Equal("DB: Healthy", dbBadge.GetAttribute("aria-label"));
    }

    [Fact]
    public void StatusGreetingCard_WhenDbError_ShowsRedBadgeForDb()
    {
        SetupMixedStates();
        var authState = Task.FromResult(CreateAuthState());

        var cut = RenderComponent<StatusGreetingCard>(parameters => parameters
            .AddCascadingValue(authState));

        var dbBadge = cut.Find("[data-testid='status-badge-db']");

        // DB badge should be Error (red)
        Assert.Contains("status-badge--error", dbBadge.ClassName);
        Assert.Equal("DB: Error", dbBadge.GetAttribute("aria-label"));

        // API should remain green
        var apiBadge = cut.Find("[data-testid='status-badge-api']");
        Assert.Contains("status-badge--healthy", apiBadge.ClassName);
    }

    [Fact]
    public void StatusGreetingCard_WhenApiFails_ShowsErrorGreetingAndGreyBadges()
    {
        _apiClient.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SystemStatusResponse>(new HttpRequestException("API down")));
        var authState = Task.FromResult(CreateAuthState());

        var cut = RenderComponent<StatusGreetingCard>(parameters => parameters
            .AddCascadingValue(authState));

        // Should show "System status unavailable" greeting
        var greeting = cut.Find("[data-testid='status-greeting-text']");
        Assert.Contains("System status unavailable", greeting.TextContent);

        // Badges should still render, all grey/offline
        var apiBadge = cut.Find("[data-testid='status-badge-api']");
        Assert.Contains("status-badge--offline", apiBadge.ClassName);
        Assert.Equal("API: Pending", apiBadge.GetAttribute("aria-label"));
    }

    [Fact]
    public void StatusGreetingCard_ShowsGreetingWithUserName()
    {
        SetupAllOk();
        var authState = Task.FromResult(CreateAuthState("Alice"));

        var cut = RenderComponent<StatusGreetingCard>(parameters => parameters
            .AddCascadingValue(authState));

        var greeting = cut.Find("[data-testid='status-greeting-text']");
        Assert.Contains("Alice", greeting.TextContent);
    }

    [Fact]
    public void StatusGreetingCard_AllBadgesHaveAccessibleLabels()
    {
        SetupAllOk();
        var authState = Task.FromResult(CreateAuthState());

        var cut = RenderComponent<StatusGreetingCard>(parameters => parameters
            .AddCascadingValue(authState));

        var apiBadge = cut.Find("[data-testid='status-badge-api']");
        var dbBadge = cut.Find("[data-testid='status-badge-db']");
        var authBadge = cut.Find("[data-testid='status-badge-auth']");
        var qdrantBadge = cut.Find("[data-testid='status-badge-qdrant']");
        var llmBadge = cut.Find("[data-testid='status-badge-llm']");

        Assert.NotNull(apiBadge.GetAttribute("aria-label"));
        Assert.NotNull(dbBadge.GetAttribute("aria-label"));
        Assert.NotNull(authBadge.GetAttribute("aria-label"));
        Assert.NotNull(qdrantBadge.GetAttribute("aria-label"));
        Assert.NotNull(llmBadge.GetAttribute("aria-label"));

        // Verify the format: "{indicator name}: Healthy|Warning|Error|Pending"
        Assert.Matches("^\\w+: (Healthy|Warning|Error|Pending)$", apiBadge.GetAttribute("aria-label")!);
        Assert.Matches("^\\w+: (Healthy|Warning|Error|Pending)$", dbBadge.GetAttribute("aria-label")!);
    }
}
