using System.Net;
using System.Text.Json;
using Aura.UI;
using Aura.UI.Models;
using Aura.UI.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aura.E2E.Dashboard;

/// <summary>
/// HTTP-only smoke tests for the Aura.UI dashboard shell.
/// These tests use <see cref="WebApplicationFactory{TEntryPoint}"/> to exercise the
/// Blazor Server host over HTTP — they verify rendered HTML markers and content, NOT
/// browser interactions. Playwright is not configured in this repository; all E2E
/// coverage here is non-Playwright, host-level smoke verification.
/// </summary>
/// <remarks>
/// Boundary: Aura.UI communicates with Aura.Api exclusively through HTTP.
/// The test stubs (<see cref="StubDashboardApiClient"/>, <see cref="ThrowingDashboardApiClient"/>,
/// <see cref="DelayedDashboardApiClient"/>) replace the typed HttpClient contract so
/// no real Aura.Api instance is needed, while still proving the UI shell renders
/// loading, empty, error, and populated states from API-shaped data.
/// </remarks>
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

    [Fact]
    public async Task GetRootWithPopulatedDashboardRendersUserSummaryInHeader()
    {
        var client = CreateClient(new StubDashboardApiClient(
            new InitialDashboardResponse(
                "Header User",
                [new DashboardCardResponse("Inbox", "5 pending", "info")])));

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"dashboard-header-user\"", html);
        Assert.Contains("Header User", html);
    }

    /// <summary>
    /// Proves the real <see cref="DashboardApiClient"/> executes at runtime. Instead of
    /// replacing <see cref="IDashboardApiClient"/> with a stub, this test keeps the real
    /// typed HTTP client and only swaps the primary <see cref="HttpMessageHandler"/> to
    /// return a canned API response. This exercises the host rendering path against the
    /// real dashboard HTTP client without requiring a live Aura.Api instance.
    /// </summary>
    [Fact]
    public async Task GetRootWithRealDashboardApiClientRendersPopulatedStateFromApiResponse()
    {
        var apiPayload = new InitialDashboardResponse(
            "Runtime Path User",
            [new DashboardCardResponse("Live Metric", "42 active", "ready")]);

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Swap only the HTTP transport — keep DashboardApiClient as the
                // real typed client implementation under test.
                services.AddHttpClient<IDashboardApiClient, DashboardApiClient>(client =>
                    {
                        client.BaseAddress = new Uri("https://api.aura.test");
                        client.Timeout = TimeSpan.FromSeconds(10);
                    })
                    .ConfigurePrimaryHttpMessageHandler(() =>
                        new StubApiPrimaryHandler(apiPayload));
            });
        });

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"dashboard-state-populated\"", html);
        Assert.Contains("Runtime Path User", html);
        Assert.Contains("Live Metric", html);
        Assert.Contains("42 active", html);
    }

    /// <summary>
    /// Proves that loading transitions to populated state within the same request flow.
    /// Uses Blazor's <c>[StreamRendering]</c> behavior: the initial render emits the loading
    /// state marker, and the streaming update appends the populated state once the async
    /// data request completes. Both markers must appear in the same HTTP response body.
    /// </summary>
    [Fact]
    public async Task GetRootWithDelayedResponseShowsLoadingThenPopulatedInSameFlow()
    {
        var client = CreateClient(new DelayedDashboardApiClient(
            TimeSpan.FromMilliseconds(100),
            new InitialDashboardResponse(
                "Transition User",
                [new DashboardCardResponse("Inbox", "3 pending", "info")])));

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The initial SSR render includes the loading state
        Assert.Contains("data-testid=\"dashboard-state-loading\"", html);

        // The streaming update (same response) replaces it with populated content
        Assert.Contains("data-testid=\"dashboard-state-populated\"", html);
        Assert.Contains("Transition User", html);
        Assert.Contains("Inbox", html);
        Assert.Contains("3 pending", html);
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

    /// <summary>
    /// Replaces the network-level HTTP transport for the <see cref="DashboardApiClient"/>
    /// typed client. Returns a canned JSON response so the real client code executes
    /// (deserialization, status check, null guard) without needing a live Aura.Api instance.
    /// </summary>
    private sealed class StubApiPrimaryHandler : HttpMessageHandler
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
        private readonly string _responseJson;

        public StubApiPrimaryHandler(InitialDashboardResponse response)
        {
            _responseJson = JsonSerializer.Serialize(response, SerializerOptions);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responseJson, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }
}
