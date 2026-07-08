using System.Security.Claims;
using Aura.UI.Components.Dashboard;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Net;

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
        Services.AddSingleton<IDashboardEventBus>(new DashboardEventBus());
        Services.AddSingleton<IDashboardRealtimeStatus>(new DashboardRealtimeStatus());

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient(new StubHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost:5180/")
        };
        httpClientFactory.CreateClient("AuraApi").Returns(httpClient);
        Services.AddSingleton(httpClientFactory);

        var summary = Substitute.For<IPrioritySummaryService>();
        summary.GetCardsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<PrioritySummaryCard>()));
        Services.AddSingleton(summary);

        var previewClient = Substitute.For<IDashboardPreviewApiClient>();
        previewClient.GetPreviewAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(preview));
        Services.AddSingleton(previewClient);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"enabled\":true}")
            });
    }

    [Fact]
    public void Dashboard_NoLongerRendersTopPrioritySummaryPanel()
    {
        SetupServices(new DashboardPreviewResponse([], [])
        {
            TotalPendingCount = 4,
            HighPriorityCount = 4,
            TopItems =
            [
                new InboxItemPreviewResponse("A", "messages", "1m ago", 0.9, "Review") { PriorityScore = 95 }
            ]
        });

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<PriorityDashboard>(p => p.AddCascadingValue(authStateTask));

        Assert.DoesNotContain("priority-dashboard-top-items", cut.Markup);
        Assert.DoesNotContain("priority-dashboard-counts", cut.Markup);
    }

    [Fact]
    public void Dashboard_HeaderReplacesLiveSyncWithTopPriorityCounter_AndNavigatesOnClick()
    {
        SetupServices(new DashboardPreviewResponse([], [])
        {
            TotalPendingCount = 9,
            HighPriorityCount = 4,
            TopItems = []
        });

        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());
        var cut = RenderComponent<PriorityDashboard>(p => p.AddCascadingValue(authStateTask));

        var counter = cut.Find("[data-testid='priority-dashboard-top-priority-counter']");
        Assert.Contains("9 pending", counter.TextContent);
        Assert.Contains("4 high priority", counter.TextContent);
        Assert.DoesNotContain("Live Sync", cut.Markup);

        counter.Click();

        var navManager = Services.GetRequiredService<NavigationManager>();
        Assert.EndsWith("/top-priority", navManager.Uri, StringComparison.Ordinal);
    }
}
