using System.Security.Claims;
using Aura.UI.Components.Dashboard;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

/// <summary>
/// Tests that <see cref="PriorityDashboard"/> renders <see cref="PrioritySummaryCards"/>
/// with its three source-based cards and no legacy health panels.
/// </summary>
public class PriorityDashboardRenderOrderTests : TestContext
{
    private sealed class AlwaysAuthorizedService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Success());

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(AuthorizationResult.Success());
    }

    private void SetupCommonServices()
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, AlwaysAuthorizedService>();

        var priorityService = Substitute.For<IPrioritySummaryService>();
        priorityService.GetCardsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<PrioritySummaryCard>
            {
                new("Teams Mentions", "groups", "teams", "NEW", "items", "Open Teams",
                    "https://teams.microsoft.com", "/teams", [], null),
                new("Outlook", "mail", "outlook", "UNREAD", "items", "Open Outlook",
                    "https://outlook.office.com", "/outlook", [], null),
                new("Schedule Today", "calendar_today", "schedule", "EVENTS", "meetings", "Open Calendar",
                    "https://outlook.office.com/calendar/view/day", "/calendar/day", null, [])
            }));
        Services.AddSingleton<IPrioritySummaryService>(priorityService);

        var previewClient = Substitute.For<IDashboardPreviewApiClient>();
        previewClient.GetPreviewAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DashboardPreviewResponse([], [])
            {
                TotalPendingCount = 0,
                HighPriorityCount = 0,
                TopItems = []
            }));
        Services.AddSingleton(previewClient);
    }

    private static AuthenticationState CreateAuthorizedState()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "Test User")],
            "TestAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private void RenderPriorityDashboard(out string markup)
    {
        SetupCommonServices();

        var state = new DashboardViewState(
            DashboardViewStateKind.Populated, "Test User", [], "Ready");

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());

        var cut = RenderComponent<PriorityDashboard>(parameters => parameters
            .AddCascadingValue(authStateTask)
            .AddCascadingValue(state));

        markup = cut.Markup;
    }

    [Fact]
    public void PriorityDashboard_RendersPrioritySummaryCards()
    {
        RenderPriorityDashboard(out var markup);
        Assert.Contains("priority-summary-cards", markup);
    }

    [Fact]
    public void PriorityDashboard_RendersThreeSourceCards()
    {
        RenderPriorityDashboard(out var markup);
        Assert.Contains("data-source=\"teams\"", markup);
        Assert.Contains("data-source=\"outlook\"", markup);
        Assert.Contains("data-source=\"schedule\"", markup);
    }

    [Fact]
    public void PriorityDashboard_DoesNotRenderHealthPanels()
    {
        RenderPriorityDashboard(out var markup);
        Assert.DoesNotContain("graph-connector-panel", markup);
        Assert.DoesNotContain("system-status-panel", markup);
        Assert.DoesNotContain("module-progress-panel", markup);
    }
}
