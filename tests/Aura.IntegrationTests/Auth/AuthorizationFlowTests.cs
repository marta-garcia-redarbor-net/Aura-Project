using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aura.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aura.IntegrationTests.Auth;

/// <summary>
/// Integration tests for the authentication and authorization flow.
/// Validates 401/200 behavior and mock-login JWT issuance using <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public class AuthorizationFlowTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public AuthorizationFlowTests(WebApplicationFactory<ApiMarker> factory)
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
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MockLogin_InDevelopment_ReturnsValidJwt()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/auth/mock-login", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var token = json.RootElement.GetProperty("token").GetString();

        Assert.False(string.IsNullOrEmpty(token));
        // JWT format: header.payload.signature
        Assert.Equal(3, token!.Split('.').Length);
    }

    [Fact]
    public async Task MockLogin_InProduction_ReturnsValidToken()
    {
        // Arrange — production environment: mock-login is now available in all environments
        var prodFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
        });
        var client = prodFactory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/auth/mock-login", null);

        // Assert — mock-login works in all environments (no longer gated by IsDevelopment)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var token = json.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(token));
        Assert.Equal(3, token!.Split('.').Length);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMockToken_Returns200WithUser()
    {
        // Arrange — obtain a mock token
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsync("/api/auth/mock-login", null);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        using var loginJson = JsonDocument.Parse(loginContent);
        var token = loginJson.RootElement.GetProperty("token").GetString()!;
        Assert.False(string.IsNullOrWhiteSpace(token));

        // Act — call a protected endpoint with the token
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var meContent = await response.Content.ReadAsStringAsync();
        using var meJson = JsonDocument.Parse(meContent);
        var userId = meJson.RootElement.GetProperty("userId").GetString();
        Assert.Equal("mock-user-001", userId);
    }
}
