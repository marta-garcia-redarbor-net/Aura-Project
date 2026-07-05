using Aura.UI.Pages;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Security.Claims;

namespace Aura.UnitTests.Dashboard;

public class TopPriorityPageTests : TestContext
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

    private void SetupServices()
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, AlwaysAuthorizedService>();

        var workItemsApi = Substitute.For<IWorkItemsApiClient>();

        var teams = new List<WorkItemDetailResponse>
        {
            new(Guid.NewGuid(), "teams-1", "Teams top older", "teams", "TeamsMessage", "Pending", "High", "5m", new DateTimeOffset(2026, 7, 5, 11, 0, 0, TimeSpan.Zero)) { PriorityScore = 80 },
            new(Guid.NewGuid(), "teams-2", "Teams top newer", "teams", "TeamsMessage", "Pending", "High", "2m", new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero)) { PriorityScore = 80 }
        };

        var outlook = new List<WorkItemDetailResponse>
        {
            new(Guid.NewGuid(), "outlook-1", "Outlook medium", "outlook", "OutlookEmail", "Pending", "Medium", "1h", new DateTimeOffset(2026, 7, 5, 10, 0, 0, TimeSpan.Zero)) { PriorityScore = 50 }
        };

        var pr = new List<WorkItemDetailResponse>
        {
            new(Guid.NewGuid(), "pr-1", "PR low", "pr", "PrReview", "Pending", "Low", "3h", new DateTimeOffset(2026, 7, 5, 9, 0, 0, TimeSpan.Zero)) { PriorityScore = 20 }
        };

        workItemsApi.GetBySourceAsync(Arg.Any<string>(), "Pending", Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var sourceType = ci.ArgAt<string>(0);
                return sourceType switch
                {
                    "TeamsMessage" => Task.FromResult<IReadOnlyList<WorkItemDetailResponse>>(teams),
                    "OutlookEmail" => Task.FromResult<IReadOnlyList<WorkItemDetailResponse>>(outlook),
                    "PrReview" => Task.FromResult<IReadOnlyList<WorkItemDetailResponse>>(pr),
                    _ => Task.FromResult<IReadOnlyList<WorkItemDetailResponse>>([])
                };
            });

        Services.AddSingleton(workItemsApi);
    }

    [Fact]
    public void TopPriority_RendersFlatSortedList_ByPriorityThenRecency_WithoutGroupingContainers()
    {
        SetupServices();

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<TopPriority>(p => p.AddCascadingValue(authStateTask));

        var renderedItems = cut.FindAll("[data-testid='top-priority-item']");
        Assert.Equal(4, renderedItems.Count);

        Assert.Contains("Teams top newer", renderedItems[0].TextContent);
        Assert.Contains("Teams top older", renderedItems[1].TextContent);
        Assert.Contains("Outlook medium", renderedItems[2].TextContent);
        Assert.Contains("PR low", renderedItems[3].TextContent);

        Assert.DoesNotContain("dashboard-preview-source-group", cut.Markup);
        Assert.DoesNotContain("inbox-preview-group", cut.Markup);
    }

    [Fact]
    public void TopPriority_UsesSingleColumnLayout_AndHasNoRightPanel()
    {
        SetupServices();

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<TopPriority>(p => p.AddCascadingValue(authStateTask));

        Assert.NotNull(cut.Find("[data-testid='top-priority-single-column']"));
        Assert.DoesNotContain("right-panel", cut.Markup);
        Assert.DoesNotContain("top-priority-layout--with-sidebar", cut.Markup);
    }

    [Fact]
    public void TopPriority_DetailView_UsesStitchDetailLayout_WithoutRightPanel()
    {
        SetupServices();

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<TopPriority>(p => p.AddCascadingValue(authStateTask));

        Assert.NotNull(cut.Find("[data-testid='top-priority-detail-layout']"));
        Assert.NotNull(cut.Find("[data-testid='top-priority-detail-item']"));
        Assert.NotNull(cut.Find("[data-testid='top-priority-item-score']"));
        Assert.DoesNotContain("right-panel", cut.Markup);
        Assert.DoesNotContain("top-priority-layout--with-sidebar", cut.Markup);
    }
}
