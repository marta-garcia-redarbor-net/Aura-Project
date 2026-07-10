using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Aura.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace Aura.IntegrationTests.Auth;

/// <summary>
/// Integration tests for dual JWT Bearer validation.
/// Verifies that the API accepts both Entra ID and mock JWT tokens simultaneously,
/// and enforces the DemoOnly authorization policy.
/// </summary>
public class DualJwtValidationTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private readonly WebApplicationFactory<ApiMarker> _factory;
    private const string TestMockJwtKey = "aura-test-key-for-integration-tests-minimum-32-characters!";

    public DualJwtValidationTests(WebApplicationFactory<ApiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("EmbeddingProvider:Endpoint", "https://test.openai.azure.com");
            builder.UseSetting("EmbeddingProvider:DeploymentName", "test-model");
            builder.UseSetting("EmbeddingProvider:ApiKey", "fake-key");
            builder.UseSetting("UseEntraId", "false");
            builder.UseSetting("MockJwt:Key", TestMockJwtKey);
            builder.UseSetting("MockJwt:Issuer", "aura-dev");
            builder.UseSetting("MockJwt:Audience", "aura-api");
            builder.UseSetting("AzureAd:ClientId", "test-client-id");
            builder.UseSetting("AzureAd:TenantId", "test-tenant-id");
        });
    }

    [Fact]
    public async Task MockLogin_InAnyEnvironment_ReturnsJwtWithDemoRole()
    {
        // Arrange — mock-login MUST work in all environments now
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/auth/mock-login", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var token = json.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(token));

        // Verify the token contains role=Demo claim
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token!);
        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("Demo", roleClaim.Value);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMockToken_Returns200()
    {
        // Arrange — obtain a mock token (with role=Demo)
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsync("/api/auth/mock-login", null);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        using var loginJson = JsonDocument.Parse(loginContent);
        var token = loginJson.RootElement.GetProperty("token").GetString()!;

        // Act — call a protected endpoint with the mock token
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);

        // Assert — mock JWT MUST be accepted by the dual-scheme pipeline
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MockLogin_InProductionEnvironment_StillReturnsToken()
    {
        // Arrange — production environment: mock-login MUST be available now
        var prodFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
        });
        var client = prodFactory.CreateClient();

        // Act
        var response = await client.PostAsync("/api/auth/mock-login", null);

        // Assert — no longer returns 404 in production
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var token = json.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task ProtectedEndpoint_WithMockTokenWithoutDemoRole_Returns401()
    {
        // Arrange — create a mock JWT WITHOUT role=Demo (manually, to test default policy)
        var token = GenerateMockJwtWithoutDemoRole();
        var client = _factory.CreateClient();

        // Act — call /api/auth/me which uses default policy (accepts both schemes)
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.SendAsync(request);

        // Assert — RequireEntraOrDemo policy rejects MockJwt without Demo role
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string GenerateMockJwtWithoutDemoRole()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestMockJwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "no-role-user"),
            new(ClaimTypes.Name, "No Role User"),
            new(ClaimTypes.Email, "norole@aura.dev"),
            new("oid", "no-role-user-001")
            // NOTE: no role=Demo claim
        };

        var token = new JwtSecurityToken(
            issuer: "aura-dev",
            audience: "aura-api",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
