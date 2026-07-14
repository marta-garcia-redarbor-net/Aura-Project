using System.Net;
using Aura.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aura.IntegrationTests.Middleware;

public class RateLimitingIntegrationTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public RateLimitingIntegrationTests(WebApplicationFactory<ApiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("EmbeddingProvider:Endpoint", "https://test.openai.azure.com");
            builder.UseSetting("EmbeddingProvider:DeploymentName", "test-model");
            builder.UseSetting("EmbeddingProvider:ApiKey", "fake-key");
            builder.UseSetting("UseEntraId", "false");
            builder.UseSetting("MockJwt:Key",
                "aura-test-key-for-integration-tests-minimum-32-characters!");
            builder.UseSetting("MockJwt:Issuer", "aura-dev");
            builder.UseSetting("MockJwt:Audience", "aura-api");
            // Low limits for testing — 5 requests per 60s window
            builder.UseSetting("RateLimiting:Default:PermitLimit", "5");
            builder.UseSetting("RateLimiting:Default:WindowSeconds", "60");
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "3");
            builder.UseSetting("RateLimiting:Auth:WindowSeconds", "60");
        });
    }

    [Fact]
    public async Task DefaultPolicy_RequestWithinLimit_Succeeds()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DefaultPolicy_ExhaustQuota_Returns429WithRetryAfter()
    {
        var client = _factory.CreateClient();

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 10; i++)
        {
            lastResponse = await client.GetAsync("/");
            if (lastResponse.StatusCode == (HttpStatusCode)429)
                break;
        }

        Assert.NotNull(lastResponse);
        Assert.Equal((HttpStatusCode)429, lastResponse.StatusCode);
        Assert.True(lastResponse.Headers.Contains("Retry-After"),
            "429 response should include a Retry-After header");
    }

    [Fact]
    public async Task AuthPolicy_ExhaustStrictQuota_Returns429()
    {
        var client = _factory.CreateClient();

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 8; i++)
        {
            lastResponse = await client.PostAsync("/api/auth/mock-login", null);
            if (lastResponse.StatusCode == (HttpStatusCode)429)
                break;
        }

        Assert.NotNull(lastResponse);
        Assert.Equal((HttpStatusCode)429, lastResponse.StatusCode);
    }
}
