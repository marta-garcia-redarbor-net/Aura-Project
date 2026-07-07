using System.Net;
using Aura.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aura.IntegrationTests.Middleware;

public class SecurityHeadersIntegrationTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public SecurityHeadersIntegrationTests(WebApplicationFactory<ApiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("EmbeddingProvider:Endpoint", "https://test.openai.azure.com");
            builder.UseSetting("EmbeddingProvider:DeploymentName", "test-model");
            builder.UseSetting("EmbeddingProvider:ApiKey", "fake-key");
            builder.UseSetting("UseEntraId", "false");
            builder.UseSetting("MockJwt:Key",
                "aura-test-key-for-integration-tests-minimum-32-characters!");
        });
    }

    [Fact]
    public async Task OkResponse_IncludesAllSecurityHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("default-src 'self'",
            response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task NotFoundResponse_IncludesSecurityHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/nonexistent-path-xyz");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("default-src 'self'",
            response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task DevelopmentEnvironment_DoesNotIncludeHsts()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Strict-Transport-Security"),
            "HSTS should not be present in Development environment");
    }

    [Fact]
    public async Task ProductionEnvironment_IncludesHstsHeader()
    {
        var productionFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
        });
        var client = productionFactory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Strict-Transport-Security"),
            "HSTS should be present in Production environment");
        Assert.Equal("max-age=31536000; includeSubDomains",
            response.Headers.GetValues("Strict-Transport-Security").Single());
    }
}
