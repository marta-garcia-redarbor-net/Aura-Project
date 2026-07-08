using System.Security.Claims;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.Pages;

public class PullRequestsPageTests : TestContext
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
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "Test User")],
            "TestAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private void SetupAuthorization()
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, AlwaysAuthorizedService>();
    }

    [Fact]
    public void LoadingState_ShowsLoadingIndicator()
    {
        // Arrange
        SetupAuthorization();
        var prClient = Substitute.For<IPullRequestsApiClient>();
        prClient.GetPendingPullRequestsAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.Delay(5000, _.Arg<CancellationToken>())
                .ContinueWith(_ => (IReadOnlyList<PullRequestResponse>)Array.Empty<PullRequestResponse>()));
        Services.AddSingleton(prClient);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());

        // Act
        var cut = RenderComponent<Aura.UI.Pages.PullRequests>(parameters => parameters
            .AddCascadingValue(authStateTask));

        // Assert
        Assert.NotNull(cut.Find("[data-testid='pr-loading']"));
    }

    [Fact]
    public void EmptyState_ShowsNoPendingPRsMessage()
    {
        // Arrange
        SetupAuthorization();
        var prClient = Substitute.For<IPullRequestsApiClient>();
        prClient.GetPendingPullRequestsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PullRequestResponse>>(Array.Empty<PullRequestResponse>()));
        Services.AddSingleton(prClient);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());

        // Act
        var cut = RenderComponent<Aura.UI.Pages.PullRequests>(parameters => parameters
            .AddCascadingValue(authStateTask));

        // Assert
        cut.WaitForElement("[data-testid='pr-empty']", TimeSpan.FromSeconds(3));
        Assert.Contains("No pending PRs", cut.Markup);
    }

    [Fact]
    public void PopulatedState_RendersPRTable()
    {
        // Arrange
        SetupAuthorization();
        var prClient = Substitute.For<IPullRequestsApiClient>();
        var prs = new List<PullRequestResponse>
        {
            new(
                Id: 142,
                Title: "Hotfix: production crash",
                RepoName: "Aura",
                Author: "Carlos Ruiz",
                CreatedAt: new DateTimeOffset(2026, 07, 1, 08, 30, 00, TimeSpan.Zero),
                UpdatedAt: new DateTimeOffset(2026, 07, 1, 09, 15, 00, TimeSpan.Zero),
                Status: "active",
                ReviewerCount: 2,
                CommentCount: 12,
                FileCount: 3,
                SourceLink: "https://dev.azure.com/auraorg/Aura/_git/Aura/pullrequest/142",
                IsDraft: false,
                Priority: "critical",
                BranchName: "main",
                SourceBranchName: "hotfix/payment-crash",
                BuildStatus: "passing",
                ReviewApprovals: 1,
                ReviewRequired: 2,
                ReviewChangesRequested: 0
            ),
            new(
                Id: 145,
                Title: "Feature: reporting dashboard v2",
                RepoName: "Aura",
                Author: "Laura Sánchez",
                CreatedAt: new DateTimeOffset(2026, 07, 1, 11, 00, 00, TimeSpan.Zero),
                UpdatedAt: new DateTimeOffset(2026, 07, 1, 14, 00, 00, TimeSpan.Zero),
                Status: "active",
                ReviewerCount: 3,
                CommentCount: 5,
                FileCount: 12,
                SourceLink: "https://dev.azure.com/auraorg/Aura/_git/Aura/pullrequest/145",
                IsDraft: false,
                Priority: "high",
                BranchName: "develop",
                SourceBranchName: "feature/reporting-v2",
                BuildStatus: "running",
                ReviewApprovals: 1,
                ReviewRequired: 1,
                ReviewChangesRequested: 0
            )
        };
        prClient.GetPendingPullRequestsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PullRequestResponse>>(prs));
        Services.AddSingleton(prClient);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());

        // Act
        var cut = RenderComponent<Aura.UI.Pages.PullRequests>(parameters => parameters
            .AddCascadingValue(authStateTask));

        // Assert
        cut.WaitForElement("[data-testid='pr-list']", TimeSpan.FromSeconds(3));
        var rows = cut.FindAll("[data-testid='pr-row']");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Hotfix: production crash", rows[0].TextContent);
        Assert.Contains("Feature: reporting dashboard v2", rows[1].TextContent);

        // Verify CI status badges
        var ciStatusBadges = cut.FindAll("[data-testid='pr-ci-status']");
        Assert.Equal(2, ciStatusBadges.Count);
        Assert.Contains("passing", ciStatusBadges[0].TextContent);
        Assert.Contains("running", ciStatusBadges[1].TextContent);

        // Verify the PR list wrapper and pagination exist
        Assert.NotNull(cut.Find("[data-testid='pr-list']"));
        Assert.NotNull(cut.Find("[data-testid='pr-pagination']"));
    }

    [Fact]
    public void PopulatedState_RendersOpenAdoLinks()
    {
        // Arrange
        SetupAuthorization();
        var prClient = Substitute.For<IPullRequestsApiClient>();
        prClient.GetPendingPullRequestsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PullRequestResponse>>(new List<PullRequestResponse>
            {
                new(
                    Id: 142,
                    Title: "Hotfix: production crash",
                    RepoName: "Aura",
                    Author: "Carlos Ruiz",
                    CreatedAt: new DateTimeOffset(2026, 07, 1, 08, 30, 00, TimeSpan.Zero),
                    UpdatedAt: new DateTimeOffset(2026, 07, 1, 09, 15, 00, TimeSpan.Zero),
                    Status: "active",
                    ReviewerCount: 2,
                    CommentCount: 12,
                    FileCount: 3,
                    SourceLink: "https://dev.azure.com/auraorg/Aura/_git/Aura/pullrequest/142",
                    IsDraft: false,
                    Priority: "critical",
                    BranchName: "main",
                    SourceBranchName: "hotfix/payment-crash",
                    BuildStatus: "passing",
                    ReviewApprovals: 1,
                    ReviewRequired: 2,
                    ReviewChangesRequested: 0
                )
            }));
        Services.AddSingleton(prClient);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());

        var cut = RenderComponent<Aura.UI.Pages.PullRequests>(parameters => parameters
            .AddCascadingValue(authStateTask));

        cut.WaitForElement("[data-testid='pr-list']", TimeSpan.FromSeconds(3));
        var links = cut.FindAll("[data-testid='pr-open-link']");
        Assert.Single(links);
        Assert.Contains("https://dev.azure.com", links[0].GetAttribute("href"));
    }

    [Fact]
    public void ErrorState_ShowsErrorMessageAndRetryButton()
    {
        // Arrange
        SetupAuthorization();
        var prClient = Substitute.For<IPullRequestsApiClient>();
        prClient.GetPendingPullRequestsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<PullRequestResponse>>(new HttpRequestException("Connection failed")));
        Services.AddSingleton(prClient);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());

        // Act
        var cut = RenderComponent<Aura.UI.Pages.PullRequests>(parameters => parameters
            .AddCascadingValue(authStateTask));

        // Assert
        cut.WaitForElement("[data-testid='pr-error']", TimeSpan.FromSeconds(3));
        Assert.NotNull(cut.Find("[data-testid='pr-retry-btn']"));
        Assert.Contains("Failed to load PRs", cut.Markup);
    }

    [Fact]
    public void PopulatedState_ShowsPendingCount()
    {
        // Arrange
        SetupAuthorization();
        var prClient = Substitute.For<IPullRequestsApiClient>();
        prClient.GetPendingPullRequestsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PullRequestResponse>>(new List<PullRequestResponse>
            {
                new(142, "PR 1", "Aura", "Alice",
                    DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow,
                    "active", 1, 0, 1, "https://dev.azure.com/pr/142", false, "high",
                    "main", "feature/foo", "passing", 1, 1, 0),
                new(143, "PR 2", "Aura", "Bob",
                    DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow,
                    "active", 2, 3, 5, "https://dev.azure.com/pr/143", false, "medium",
                    "develop", "fix/bar", "running", 0, 2, 1)
            }));
        Services.AddSingleton(prClient);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());

        // Act
        var cut = RenderComponent<Aura.UI.Pages.PullRequests>(parameters => parameters
            .AddCascadingValue(authStateTask));

        // Assert
        cut.WaitForElement("[data-testid='pr-pending-count']", TimeSpan.FromSeconds(3));
        Assert.Contains("2 pending", cut.Find("[data-testid='pr-pending-count']").TextContent);
    }

    [Fact]
    public void PrPreviewItemResponse_ConstructsCorrectly()
    {
        // Arrange & Act
        var item = new PrPreviewItemResponse(
            Title: "Test PR",
            PrDisplayName: "#42 Test PR",
            BranchName: "feature/test",
            BuildStatus: "passing",
            ReviewApprovals: 2,
            ReviewRequired: 3,
            ReviewChangesRequested: 0,
            Author: "Test User",
            UpdatedAt: new DateTimeOffset(2026, 07, 1, 12, 0, 0, TimeSpan.Zero),
            RelativeTimestamp: "2h ago",
            SourceLink: "https://dev.azure.com/pr/42",
            IsDraft: false,
            Priority: "high");

        // Assert
        Assert.Equal("Test PR", item.Title);
        Assert.Equal("#42 Test PR", item.PrDisplayName);
        Assert.Equal("passing", item.BuildStatus);
        Assert.Equal("feature/test", item.BranchName);
        Assert.Equal(2, item.ReviewApprovals);
        Assert.Equal(3, item.ReviewRequired);
        Assert.Equal("Test User", item.Author);
        Assert.Equal("2h ago", item.RelativeTimestamp);
        Assert.Equal("high", item.Priority);
        Assert.False(item.IsDraft);
    }
}
