using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Aura.UI;
using Aura.UI.Models;
using Aura.UI.Services;
using Aura.E2E.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.E2E.PullRequests;

/// <summary>
/// HTTP-only smoke tests for the PullRequests page.
/// Uses TestAuthHandler to bypass cookie auth and authenticate the request
/// at the ASP.NET Core middleware level (required for [Authorize] on the page).
/// Blazor-level auth is handled by AddAuthenticatedUiTestUser().
/// </summary>
public class PullRequestsPageSmokeTests : IClassFixture<WebApplicationFactory<UiMarker>>
{
    private readonly WebApplicationFactory<UiMarker> _factory;

    public PullRequestsPageSmokeTests(WebApplicationFactory<UiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("AuraApi:BaseUrl", "https://api.aura.test");
        });
    }

    [Fact]
    public async Task GetPullRequestsPage_RendersPRList()
    {
        var client = CreateClientWithPrData();

        var response = await client.GetAsync("/pull-requests");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"pr-list\"", html);
        Assert.Contains("data-testid=\"pr-row\"", html);
        Assert.Contains("data-testid=\"pr-open-link\"", html);
        Assert.Contains("data-testid=\"pr-pagination\"", html);
        Assert.Contains("data-testid=\"pr-ci-status\"", html);
    }

    [Fact]
    public async Task GetPullRequestsPage_RendersPendingCount()
    {
        var client = CreateClientWithPrData();

        var response = await client.GetAsync("/pull-requests");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"pr-pending-count\"", html);
        Assert.Contains("2 pending", html);
    }

    [Fact]
    public async Task GetPullRequestsPage_EmptyState_RendersNoPRsMessage()
    {
        var client = CreateClientWithEmptyPrData();

        var response = await client.GetAsync("/pull-requests");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"pr-empty\"", html);
        Assert.Contains("No pending PRs", html);
    }

    [Fact]
    public async Task GetPullRequestsPage_ErrorState_RendersRetryButton()
    {
        var client = CreateClientWithFailingPrData();

        var response = await client.GetAsync("/pull-requests");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"pr-error\"", html);
        Assert.Contains("data-testid=\"pr-retry-btn\"", html);
    }

    private HttpClient CreateClientWithPrData()
    {
        var prs = new List<PullRequestResponse>
        {
            new PullRequestResponse(
                142,
                "Hotfix: production crash",
                "Aura",
                "Carlos Ruiz",
                new DateTimeOffset(2026, 07, 1, 08, 30, 00, TimeSpan.Zero),
                new DateTimeOffset(2026, 07, 1, 09, 15, 00, TimeSpan.Zero),
                "active",
                2,
                12,
                3,
                "https://dev.azure.com/auraorg/Aura/_git/Aura/pullrequest/142",
                false,
                "critical",
                "main",
                "hotfix/payment-crash",
                "passing",
                1,
                2,
                0,
                "direct"),
            new PullRequestResponse(
                145,
                "Feature: reporting dashboard v2",
                "Aura",
                "Laura Sánchez",
                new DateTimeOffset(2026, 07, 1, 11, 00, 00, TimeSpan.Zero),
                new DateTimeOffset(2026, 07, 1, 14, 00, 00, TimeSpan.Zero),
                "active",
                3,
                5,
                12,
                "https://dev.azure.com/auraorg/Aura/_git/Aura/pullrequest/145",
                false,
                "high",
                "develop",
                "feature/reporting-v2",
                "running",
                1,
                1,
                0,
                "direct")
        };

        return CreateClient(new StubPrClient(prs));
    }

    private HttpClient CreateClientWithEmptyPrData()
    {
        return CreateClient(new StubPrClient([]));
    }

    private HttpClient CreateClientWithFailingPrData()
    {
        return CreateClient(new ThrowingPrClient());
    }

    private HttpClient CreateClient(IAzureDevOpsPrClient prClient)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Override the default auth scheme with a test handler that always succeeds.
                // This is required because /pull-requests has [Authorize] at the page level,
                // which triggers the ASP.NET Core authorization middleware.
                // The cookie scheme from Program.cs would redirect to login without a real cookie.
                services.AddAuthentication(AuthenticationSchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        AuthenticationSchemeName, _ => { });

                services.AddAuthenticatedUiTestUser();

                services.AddStubFocusStateApiClient();
                services.RemoveAll<IAzureDevOpsPrClient>();
                services.AddScoped(_ => prClient);

                // The page now uses IPullRequestsApiClient — bridge from the existing stub
                services.RemoveAll<IPullRequestsApiClient>();
                services.AddScoped<IPullRequestsApiClient>(_ => new PullRequestsApiClientBridge(prClient));

                // Stub out other required services
                services.RemoveAll<IDashboardPreviewApiClient>();
                services.RemoveAll<ICalendarApiClient>();
                services.RemoveAll<IPrioritySummaryService>();
                services.AddScoped<IDashboardPreviewApiClient>(_ =>
                    new StubPreviewClient(new DashboardPreviewResponse([], [])));
                services.AddScoped<ICalendarApiClient>(_ =>
                    new StubCalendarClient([]));
                services.AddScoped<IPrioritySummaryService>(_ =>
                    new StubPrioritySummaryService());
            });
        });

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private const string AuthenticationSchemeName = "TestE2E";

    /// <summary>
    /// Test authentication handler that always succeeds.
    /// Bypasses cookie auth so we can test [Authorize]-protected pages via HTTP.
    /// </summary>
    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-001"),
                new Claim(ClaimTypes.Name, "Test User"),
            };
            var identity = new ClaimsIdentity(claims, AuthenticationSchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationSchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class StubPrClient : IAzureDevOpsPrClient
    {
        private readonly List<PullRequestResponse> _prs;
        public StubPrClient(List<PullRequestResponse> prs) => _prs = prs;
        public Task<List<PullRequestResponse>> GetPendingPullRequestsAsync(CancellationToken ct)
            => Task.FromResult(_prs);
    }

    private sealed class ThrowingPrClient : IAzureDevOpsPrClient
    {
        public Task<List<PullRequestResponse>> GetPendingPullRequestsAsync(CancellationToken ct)
            => Task.FromException<List<PullRequestResponse>>(new HttpRequestException("Connection refused"));
    }

    /// <summary>
    /// Bridge from legacy IAzureDevOpsPrClient to IPullRequestsApiClient for E2E tests.
    /// Preserves existing StubPrClient/ThrowingPrClient while the page uses the new port.
    /// </summary>
    private sealed class PullRequestsApiClientBridge : IPullRequestsApiClient
    {
        private readonly IAzureDevOpsPrClient _inner;
        public PullRequestsApiClientBridge(IAzureDevOpsPrClient inner) => _inner = inner;
        public async Task<IReadOnlyList<PullRequestResponse>> GetPendingPullRequestsAsync(CancellationToken ct = default)
            => await _inner.GetPendingPullRequestsAsync(ct);
    }

    private sealed class StubPreviewClient : IDashboardPreviewApiClient
    {
        private readonly DashboardPreviewResponse _response;
        public StubPreviewClient(DashboardPreviewResponse response) => _response = response;
        public Task<DashboardPreviewResponse> GetPreviewAsync(CancellationToken ct) => Task.FromResult(_response);
    }

    private sealed class StubCalendarClient : ICalendarApiClient
    {
        private readonly List<UpcomingMeetingResponse> _meetings;
        public StubCalendarClient(List<UpcomingMeetingResponse> meetings) => _meetings = meetings;
        public Task<IReadOnlyList<UpcomingMeetingResponse>> GetUpcomingMeetingsAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<UpcomingMeetingResponse>>(_meetings);
        public Task<IReadOnlyList<UpcomingMeetingResponse>> GetTodayCalendarAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<UpcomingMeetingResponse>>(_meetings);
    }

    private sealed class StubPrioritySummaryService : IPrioritySummaryService
    {
        public Task<List<PrioritySummaryCard>> GetCardsAsync(CancellationToken ct)
            => Task.FromResult(new List<PrioritySummaryCard>());
        public string FormatTimeRange(UpcomingMeetingResponse ev) => string.Empty;
        public string GetEventStatus(UpcomingMeetingResponse ev) => string.Empty;
    }
}
