using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aura.Api;
using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.IntegrationTests.Dashboard;

public class InitialDashboardEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public InitialDashboardEndpointTests(WebApplicationFactory<ApiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("EmbeddingProvider:Endpoint", "https://test.openai.azure.com");
            builder.UseSetting("EmbeddingProvider:DeploymentName", "test-model");
            builder.UseSetting("EmbeddingProvider:ApiKey", "fake-key");
            builder.UseSetting("MockJwt:Key",
                "aura-test-key-for-integration-tests-minimum-32-characters!");
        });
    }

    [Fact]
    public async Task GetInitialDashboard_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/initial");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetInitialDashboard_WithTokenAndCards_Returns200PopulatedPayload()
    {
        var client = CreateAuthenticatedClient(new InitialDashboardDto(
            "Mock User",
            [new DashboardCardDto("Signed in as", "Mock User", "info")]));

        var response = await client.GetAsync("/api/dashboard/initial");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadDashboardAsync(response);
        Assert.Equal("Mock User", payload.UserDisplayName);
        var card = Assert.Single(payload.Cards);
        Assert.Equal("Signed in as", card.Title);
        Assert.Equal("Mock User", card.Value);
        Assert.Equal("info", card.Status);
    }

    [Fact]
    public async Task GetInitialDashboard_WithTokenAndNoCards_Returns200EmptyPayload()
    {
        var client = CreateAuthenticatedClient(new InitialDashboardDto("Mock User", []));

        var response = await client.GetAsync("/api/dashboard/initial");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadDashboardAsync(response);
        Assert.Equal("Mock User", payload.UserDisplayName);
        Assert.Empty(payload.Cards);
    }

    private HttpClient CreateAuthenticatedClient(InitialDashboardDto dashboard)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IInitialDashboardReader>(new StubInitialDashboardReader(dashboard));
            });
        });

        var client = factory.CreateClient();
        var token = GetMockTokenAsync(client).GetAwaiter().GetResult();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> GetMockTokenAsync(HttpClient client)
    {
        var loginResponse = await client.PostAsync("/api/auth/mock-login", null);
        var content = await loginResponse.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        return json.RootElement.GetProperty("token").GetString()!;
    }

    private static async Task<InitialDashboardDto> ReadDashboardAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<InitialDashboardDto>(content, SerializerOptions)!;
    }

    private sealed class StubInitialDashboardReader : IInitialDashboardReader
    {
        private readonly InitialDashboardDto _dashboard;

        public StubInitialDashboardReader(InitialDashboardDto dashboard)
        {
            _dashboard = dashboard;
        }

        public Task<InitialDashboardDto> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(_dashboard);
    }
}
