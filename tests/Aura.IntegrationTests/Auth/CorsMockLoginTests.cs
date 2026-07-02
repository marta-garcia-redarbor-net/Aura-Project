using System.Net;
using Aura.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aura.IntegrationTests.Auth;

/// <summary>
/// Integration tests for CORS configuration on the mock-login endpoint.
/// Validates that cross-origin requests from the UI server are allowed.
/// </summary>
public class CorsMockLoginTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public CorsMockLoginTests(WebApplicationFactory<ApiMarker> factory)
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
    public async Task MockLogin_Preflight_ReturnsCorsHeaders()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/mock-login");
        request.Headers.Add("Origin", "http://localhost:5190");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type");

        // Act
        var response = await client.SendAsync(request);

        // Assert — ASP.NET Core CORS returns 204 NoContent for preflight
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("http://localhost:5190",
            response.Headers.GetValues("Access-Control-Allow-Origin").First());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Credentials"));
        Assert.Equal("true",
            response.Headers.GetValues("Access-Control-Allow-Credentials").First());
    }

    [Fact]
    public async Task MockLogin_CrossOriginPost_ReturnsCorsHeaders()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/mock-login");
        request.Headers.Add("Origin", "http://localhost:5190");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("http://localhost:5190",
            response.Headers.GetValues("Access-Control-Allow-Origin").First());
        Assert.True(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }
}
