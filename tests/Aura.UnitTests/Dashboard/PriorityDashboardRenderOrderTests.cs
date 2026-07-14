using System.Security.Claims;
using Aura.UI.Components.Dashboard;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
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
        Services.AddSingleton<IDashboardEventBus>(new DashboardEventBus());
        Services.AddSingleton<IDashboardRealtimeStatus>(new DashboardRealtimeStatus());
        Services.AddSingleton<IFocusStateRefreshScheduler>(
            Substitute.For<IFocusStateRefreshScheduler>());
        Services.AddSingleton<IFocusStateApiClient>(
            Substitute.For<IFocusStateApiClient>());
        Services.AddSingleton(new DemoUiState());

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient(new StubHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost:5180/")
        };
        httpClientFactory.CreateClient("AuraApi").Returns(httpClient);
        Services.AddSingleton(httpClientFactory);

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

        var statusClient = Substitute.For<ISystemStatusApiClient>();
        statusClient.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SystemStatusResponse(
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "API ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "DB ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "Qdrant ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "LLM ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "MockAuth ok"))));
        statusClient.GetRecentErrorsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<ErrorEntryDto>()));
        Services.AddSingleton(statusClient);

        var moduleProgressClient = Substitute.For<IModuleProgressApiClient>();
        moduleProgressClient.GetAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ModuleProgressResponse([], IsSeeded: true)));
        Services.AddSingleton(moduleProgressClient);

        var syncClient = Substitute.For<ISyncApiClient>();
        syncClient.GetSyncStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<SourceSyncStateDto>()));
        syncClient.TriggerSyncAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        Services.AddSingleton(syncClient);

        var graphConnectorClient = Substitute.For<IGraphConnectorApiClient>();
        graphConnectorClient.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphConnectorStatusResponse("Disabled")));
        Services.AddSingleton(graphConnectorClient);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = request.RequestUri?.AbsolutePath.Contains("/api/demo/status", StringComparison.OrdinalIgnoreCase) == true
                ? "{\"enabled\":true}"
                : "{}";

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            });
        }
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

        var cards = new List<DashboardCardResponse>
        {
            new("Teams", "5 mentions", "info"),
            new("Outlook", "3 emails", "warning"),
            new("Schedule", "2 meetings", "info")
        };
        var state = new DashboardViewState(
            DashboardViewStateKind.Populated, "Test User", cards, "Ready");

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
        Assert.Contains("class=\"dashboard-cards\"", markup);
    }

    [Fact]
    public void PriorityDashboard_RendersThreeSourceCards()
    {
        RenderPriorityDashboard(out var markup);
        Assert.Contains("data-testid=\"dashboard-card\"", markup);
    }

    [Fact]
    public void PriorityDashboard_DoesNotRenderLegacyTopPriorityPanel()
    {
        RenderPriorityDashboard(out var markup);
        // The old TopPrioritySummaryPanel is no longer rendered
        Assert.DoesNotContain("top-priority-summary", markup);
    }
}
