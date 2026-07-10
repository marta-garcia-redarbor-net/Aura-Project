using System.Security.Claims;
using Aura.UI.Models;
using Aura.UI.Pages;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

public class DecisionLogPageTests : TestContext
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

    private void SetupAuthorization()
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, AlwaysAuthorizedService>();
    }

    [Fact]
    public void ShowsLoadingState_WhileRequestIsInFlight()
    {
        SetupAuthorization();
        var client = Substitute.For<IDecisionLogApiClient>();
        client.GetDecisionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.Delay(5000, _.Arg<CancellationToken>())
                .ContinueWith(_ => new DecisionLogResponse([], 0, 1, 20)));
        Services.AddSingleton(client);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<DecisionLog>(p => p.AddCascadingValue(authStateTask));

        Assert.NotNull(cut.Find("[data-testid='decision-log-loading']"));
    }

    [Fact]
    public void ShowsEmptyState_WhenNoItemsReturned()
    {
        SetupAuthorization();
        var client = Substitute.For<IDecisionLogApiClient>();
        client.GetDecisionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DecisionLogResponse([], 0, 1, 20)));
        Services.AddSingleton(client);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<DecisionLog>(p => p.AddCascadingValue(authStateTask));

        cut.WaitForElement("[data-testid='decision-log-empty']");
        Assert.Contains("No decisions recorded yet", cut.Markup);
    }

    [Fact]
    public void ShowsErrorStateAndRetry_WhenApiFails()
    {
        SetupAuthorization();
        var client = Substitute.For<IDecisionLogApiClient>();
        client.GetDecisionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<DecisionLogResponse>(new HttpRequestException("boom")));
        Services.AddSingleton(client);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<DecisionLog>(p => p.AddCascadingValue(authStateTask));

        cut.WaitForElement("[data-testid='decision-log-error']");
        Assert.NotNull(cut.Find("[data-testid='decision-log-retry']"));
    }

    [Fact]
    public void RetryButton_TriggersReload_AfterInitialFailure()
    {
        SetupAuthorization();
        var client = Substitute.For<IDecisionLogApiClient>();
        client.GetDecisionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<DecisionLogResponse>(new HttpRequestException("boom")),
                Task.FromResult(new DecisionLogResponse([], 0, 1, 20)));
        Services.AddSingleton(client);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<DecisionLog>(p => p.AddCascadingValue(authStateTask));

        cut.WaitForElement("[data-testid='decision-log-error']");
        cut.Find("[data-testid='decision-log-retry']").Click();

        cut.WaitForElement("[data-testid='decision-log-empty']");
        client.Received(2).GetDecisionsAsync(1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ShowsPopulatedTable_WithExpectedColumnsAndRows()
    {
        SetupAuthorization();
        var client = Substitute.For<IDecisionLogApiClient>();
        client.GetDecisionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DecisionLogResponse(
                [
                    new DecisionLogItemResponse(
                        Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        "Urgent PR review",
                        "pr-review",
                        "INTERRUPT",
                        88,
                        "Urgency score exceeded threshold",
                        DateTimeOffset.Parse("2026-07-05T12:00:00Z"),
                        "WindowOfOpportunity")
                ],
                1,
                1,
                20)));
        Services.AddSingleton(client);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<DecisionLog>(p => p.AddCascadingValue(authStateTask));

        cut.WaitForElement("[data-testid='decision-log-table']");
        Assert.Contains("Timestamp", cut.Markup);
        Assert.Contains("Title", cut.Markup);
        Assert.Contains("Source", cut.Markup);
        Assert.Contains("Priority Score", cut.Markup);
        Assert.Contains("Decision", cut.Markup);
        Assert.Contains("Focus State", cut.Markup);
        Assert.Contains("Explanation", cut.Markup);
        Assert.Contains("Guardrail Outcome", cut.Markup);
        Assert.Contains("Urgent PR review", cut.Markup);
    }

    [Fact]
    public void PopulatedTable_RendersExpandableTracePanelInProgressiveOrder()
    {
        SetupAuthorization();
        var client = Substitute.For<IDecisionLogApiClient>();
        client.GetDecisionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DecisionLogResponse(
                [
                    new DecisionLogItemResponse(
                        Guid.Parse("99999999-1111-2222-3333-444444444444"),
                        "Payments incident",
                        "TeamsMessage",
                        "INTERRUPT",
                        96,
                        "Rule path from deterministic score.",
                        DateTimeOffset.Parse("2026-07-05T12:00:00Z"),
                        "WindowOfOpportunity",
                        [
                            new DecisionContextItemResponse("teams-seed-001", "Incident details", "ActivityMemory", 0.91)
                        ],
                        "LLM confirms urgency.",
                        "adjusted")
                ],
                1,
                1,
                20)));
        Services.AddSingleton(client);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<DecisionLog>(p => p.AddCascadingValue(authStateTask));

        cut.WaitForElement("[data-testid='decision-trace-details']");
        var details = cut.Find("[data-testid='decision-trace-details']");
        details.SetAttribute("open", string.Empty);

        cut.WaitForElement("[data-testid='decision-trace-section-summary']");
        cut.WaitForElement("[data-testid='decision-trace-section-rules']");
        cut.WaitForElement("[data-testid='decision-trace-section-rationale']");
        cut.WaitForElement("[data-testid='decision-trace-section-context']");

        var markup = cut.Markup;
        var summaryIndex = markup.IndexOf("decision-trace-section-summary", StringComparison.Ordinal);
        var rulesIndex = markup.IndexOf("decision-trace-section-rules", StringComparison.Ordinal);
        var rationaleIndex = markup.IndexOf("decision-trace-section-rationale", StringComparison.Ordinal);
        var contextIndex = markup.IndexOf("decision-trace-section-context", StringComparison.Ordinal);

        Assert.True(summaryIndex >= 0 && rulesIndex > summaryIndex && rationaleIndex > rulesIndex && contextIndex > rationaleIndex);
    }

    [Fact]
    public void PaginationAppears_WhenMoreThanOnePageExists()
    {
        SetupAuthorization();
        var client = Substitute.For<IDecisionLogApiClient>();
        client.GetDecisionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DecisionLogResponse(
                [
                    new DecisionLogItemResponse(
                        Guid.Parse("11111111-2222-3333-4444-555555555555"),
                        "Queued review",
                        "pr-review",
                        "QUEUE",
                        72,
                        "Queue for later",
                        DateTimeOffset.Parse("2026-07-05T10:00:00Z"),
                        "WindowOfOpportunity")
                ],
                TotalCount: 50,
                Page: 1,
                PageSize: 20)));
        Services.AddSingleton(client);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<DecisionLog>(p => p.AddCascadingValue(authStateTask));

        cut.WaitForElement("[data-testid='decision-log-pagination']");
        var pager = cut.Find("[data-testid='decision-log-pagination']");
        Assert.Contains(">2<", pager.InnerHtml, StringComparison.Ordinal);
        Assert.Contains(">3<", pager.InnerHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void PaginationIsHidden_WhenSinglePageExists()
    {
        SetupAuthorization();
        var client = Substitute.For<IDecisionLogApiClient>();
        client.GetDecisionsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DecisionLogResponse(
                [
                    new DecisionLogItemResponse(
                        Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                        "Single page review",
                        "outlook",
                        "DEFER",
                        40,
                        "Deferred",
                        DateTimeOffset.Parse("2026-07-05T11:00:00Z"),
                        "Away")
                ],
                TotalCount: 10,
                Page: 1,
                PageSize: 20)));
        Services.AddSingleton(client);

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<DecisionLog>(p => p.AddCascadingValue(authStateTask));

        cut.WaitForElement("[data-testid='decision-log-table']");
        Assert.Empty(cut.FindAll("[data-testid='decision-log-pagination']"));
    }
}
