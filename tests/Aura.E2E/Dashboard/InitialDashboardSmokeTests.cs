using System.Net;
using Aura.UI;
using Aura.UI.Models;
using Aura.UI.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aura.E2E.Dashboard;

public class InitialDashboardSmokeTests : IClassFixture<WebApplicationFactory<UiMarker>>
{
    private readonly WebApplicationFactory<UiMarker> _factory;

    public InitialDashboardSmokeTests(WebApplicationFactory<UiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("AuraApi:BaseUrl", "https://api.aura.test");
        });
    }

    [Fact]
    public async Task GetRootWithSlowDashboardResponseRendersShellAndLoadingMarker()
    {
        var client = CreateClient(new DelayedDashboardApiClient(
            TimeSpan.FromMilliseconds(150),
            new InitialDashboardResponse(
                "Mock User",
                [new DashboardCardResponse("Inbox", "7 pending", "info")])));

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"dashboard-shell\"", html);
        Assert.Contains("data-testid=\"dashboard-sidebar\"", html);
        Assert.Contains("data-testid=\"dashboard-header\"", html);
        Assert.Contains("data-testid=\"dashboard-main\"", html);
        Assert.Contains("data-testid=\"dashboard-state-loading\"", html);
    }

    [Fact]
    public async Task GetRootRendersStitchAlignedDarkThemeShell()
    {
        var client = CreateClient(new StubDashboardApiClient(
            new InitialDashboardResponse("Mock User",
                [new DashboardCardResponse("Inbox", "7 pending", "info")])));

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Dark theme root class from the Stitch export
        Assert.Contains("class=\"dark\"", html);

        // Google Fonts for Inter + JetBrains Mono (design tokens from Stitch export)
        Assert.Contains("fonts.googleapis.com", html);

        // Stitch-aligned sidebar navigation items
        Assert.Contains("Dashboard", html);
        Assert.Contains("Health", html);
        Assert.Contains("Modules", html);
        Assert.Contains("Tasks", html);

        // Stitch-aligned sidebar branding
        Assert.Contains("Aura Core", html);
    }

    [Fact]
    public async Task GetRootWithEmptyDashboardRendersExplicitEmptyState()
    {
        var client = CreateClient(new StubDashboardApiClient(new InitialDashboardResponse("Mock User", [])));

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"dashboard-shell\"", html);
        Assert.Contains("data-testid=\"dashboard-state-empty\"", html);
        Assert.Contains("No dashboard items are available yet.", html);
    }

    [Fact]
    public async Task GetRootWhenDashboardRequestFailsRendersErrorStateWithoutBypassingApiBoundary()
    {
        var client = CreateClient(new ThrowingDashboardApiClient());

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"dashboard-shell\"", html);
        Assert.Contains("data-testid=\"dashboard-state-error\"", html);
        Assert.DoesNotContain("Mock User", html);
    }

    [Fact]
    public async Task GetRootWithDashboardCardsRendersPopulatedState()
    {
        var client = CreateClient(new StubDashboardApiClient(
            new InitialDashboardResponse(
                "Mock User",
                [
                    new DashboardCardResponse("Inbox", "7 pending", "info"),
                    new DashboardCardResponse("PR Review", "2 due", "warning")
                ])));

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"dashboard-state-populated\"", html);
        Assert.Contains("Mock User", html);
        Assert.Contains("Inbox", html);
        Assert.Contains("7 pending", html);
        Assert.Contains("PR Review", html);

        // Stitch-aligned card structure: each card has a status indicator marker
        Assert.Contains("data-testid=\"dashboard-card-status\"", html);
    }

    private HttpClient CreateClient(IDashboardApiClient dashboardApiClient)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IDashboardApiClient>();
                services.AddScoped(_ => dashboardApiClient);
            });
        });

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private sealed class StubDashboardApiClient : IDashboardApiClient
    {
        private readonly InitialDashboardResponse _response;

        public StubDashboardApiClient(InitialDashboardResponse response)
        {
            _response = response;
        }

        public Task<InitialDashboardResponse> GetInitialDashboardAsync(CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }

    private sealed class DelayedDashboardApiClient : IDashboardApiClient
    {
        private readonly TimeSpan _delay;
        private readonly InitialDashboardResponse _response;

        public DelayedDashboardApiClient(TimeSpan delay, InitialDashboardResponse response)
        {
            _delay = delay;
            _response = response;
        }

        public async Task<InitialDashboardResponse> GetInitialDashboardAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, cancellationToken);
            return _response;
        }
    }

    private sealed class ThrowingDashboardApiClient : IDashboardApiClient
    {
        public Task<InitialDashboardResponse> GetInitialDashboardAsync(CancellationToken cancellationToken)
            => Task.FromException<InitialDashboardResponse>(new HttpRequestException("Dashboard unavailable"));
    }
}
