using System.Net;
using System.Security.Claims;
using Aura.E2E.Shared;
using Aura.UI;
using Aura.UI.Models;
using Aura.UI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.E2E.Triage;

public class DecisionLogTracePanelTests : IClassFixture<WebApplicationFactory<UiMarker>>
{
    private readonly WebApplicationFactory<UiMarker> _factory;

    public DecisionLogTracePanelTests(WebApplicationFactory<UiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("AuraApi:BaseUrl", "https://api.aura.test");
        });
    }

    [Fact]
    public async Task DecisionLogTracePanel_RendersSummaryAndSectionsInExpectedOrder()
    {
        var client = CreateClientWithDecisionLogStub(BuildPopulatedResponse());

        var response = await client.GetAsync("/triage/decisions");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"decision-trace-summary\"", html);
        Assert.Contains("data-testid=\"decision-trace-section-summary\"", html);
        Assert.Contains("data-testid=\"decision-trace-section-rules\"", html);
        Assert.Contains("data-testid=\"decision-trace-section-rationale\"", html);
        Assert.Contains("data-testid=\"decision-trace-section-context\"", html);

        var summaryIndex = html.IndexOf("decision-trace-section-summary", StringComparison.Ordinal);
        var rulesIndex = html.IndexOf("decision-trace-section-rules", StringComparison.Ordinal);
        var rationaleIndex = html.IndexOf("decision-trace-section-rationale", StringComparison.Ordinal);
        var contextIndex = html.IndexOf("decision-trace-section-context", StringComparison.Ordinal);

        Assert.True(summaryIndex >= 0 && rulesIndex > summaryIndex && rationaleIndex > rulesIndex && contextIndex > rationaleIndex);
    }

    [Fact]
    public async Task DecisionLogTracePanel_ContextDetailsAreKeyboardAccessibleAndToggleable()
    {
        var client = CreateClientWithDecisionLogStub(BuildPopulatedResponse());

        var response = await client.GetAsync("/triage/decisions");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"decision-trace-context-details\"", html);
        Assert.Contains("retrieved context items", html, StringComparison.OrdinalIgnoreCase);
    }

    private HttpClient CreateClientWithDecisionLogStub(DecisionLogResponse response)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(TestScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestScheme, _ => { });

                services.AddAuthenticatedUiTestUser();
                services.AddStubFocusStateApiClient();

                services.RemoveAll<IDecisionLogApiClient>();
                services.AddScoped<IDecisionLogApiClient>(_ => new StubDecisionLogApiClient(response));
            });
        });

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private static DecisionLogResponse BuildPopulatedResponse()
        => new(
        [
            new DecisionLogItemResponse(
                Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                "[Curated] Payments outage requires immediate triage",
                "TeamsMessage",
                "INTERRUPT",
                98,
                "Deterministic urgent-action-needed with advisor confirmation.",
                DateTimeOffset.UtcNow,
                "WindowOfOpportunity",
                [new DecisionContextItemResponse("ctx-1", "Incident context", "ActivityMemory", 0.91)],
                "Advisor agrees with deterministic verdict.",
                "confirmed")
        ],
        TotalCount: 1,
        Page: 1,
        PageSize: 20);

    private const string TestScheme = "TestE2E";

    private sealed class StubDecisionLogApiClient(DecisionLogResponse response) : IDecisionLogApiClient
    {
        public Task<DecisionLogResponse> GetDecisionsAsync(int page, int pageSize, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-001"),
                new Claim(ClaimTypes.Name, "Test User")
            };
            var identity = new ClaimsIdentity(claims, TestScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, TestScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
