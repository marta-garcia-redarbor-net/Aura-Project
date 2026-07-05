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

public class PriorityDashboardPriorityIndicatorsTests : TestContext
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

    private static AuthenticationState CreateAuthorizedState()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "Test User")], "TestAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private void SetupServices(DashboardPreviewResponse preview)
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, AlwaysAuthorizedService>();

        var summary = Substitute.For<IPrioritySummaryService>();
        summary.GetCardsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<PrioritySummaryCard>()));
        Services.AddSingleton(summary);

        var previewClient = Substitute.For<IDashboardPreviewApiClient>();
        previewClient.GetPreviewAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(preview));
        Services.AddSingleton(previewClient);
    }

    [Fact]
    public void RendersPendingAndHighPriorityCounts()
    {
        SetupServices(new DashboardPreviewResponse([], [])
        {
            TotalPendingCount = 15,
            HighPriorityCount = 4,
            TopItems = []
        });

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<PriorityDashboard>(p => p.AddCascadingValue(authStateTask));

        cut.WaitForElement("[data-testid='priority-dashboard-counts']");
        Assert.Contains("15 pending", cut.Markup);
        Assert.Contains("4 high priority", cut.Markup);
    }

    [Fact]
    public void RendersImportanceBadges_ForTopItems()
    {
        SetupServices(new DashboardPreviewResponse([], [])
        {
            TotalPendingCount = 4,
            HighPriorityCount = 4,
            TopItems =
            [
                new InboxItemPreviewResponse("A", "messages", "1m ago", 0.9, "Review") { PriorityScore = 95 },
                new InboxItemPreviewResponse("B", "messages", "2m ago", 0.8, "Review") { PriorityScore = 90 },
                new InboxItemPreviewResponse("C", "messages", "3m ago", 0.7, "Review") { PriorityScore = 85 },
                new InboxItemPreviewResponse("D", "messages", "4m ago", 0.6, "Review") { PriorityScore = 85 }
            ]
        });

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<PriorityDashboard>(p => p.AddCascadingValue(authStateTask));

        cut.WaitForElement("[data-testid='priority-dashboard-top-items']");
        var badges = cut.FindAll("[data-testid='priority-dashboard-importance-badge']");
        Assert.Equal(4, badges.Count);
    }
}
