using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aura.Api;
using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.IntegrationTests.Dashboard;

public class SystemStatusEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public SystemStatusEndpointTests(WebApplicationFactory<ApiMarker> factory)
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
    public async Task GetSystemStatus_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/system-status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSystemStatus_WithToken_Returns200Payload()
    {
        var client = CreateAuthenticatedClient(new SystemStatusDto(
            new SystemIndicatorDto(SystemIndicatorState.Ok, "api ok"),
            new SystemIndicatorDto(SystemIndicatorState.Warning, "qdrant warn"),
            new SystemIndicatorDto(SystemIndicatorState.Error, "auth err")));

        var response = await client.GetAsync("/api/dashboard/system-status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ReadAsync(response);

        Assert.Equal(SystemIndicatorState.Ok, payload.Api.State);
        Assert.Equal("api ok", payload.Api.Microcopy);
        Assert.Equal(SystemIndicatorState.Warning, payload.Qdrant.State);
        Assert.Equal(SystemIndicatorState.Error, payload.MockAuth.State);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task WriteVerbs_AreRejectedWith405(string method)
    {
        var client = CreateAuthenticatedClient(new SystemStatusDto(
            new SystemIndicatorDto(SystemIndicatorState.Ok, "api ok"),
            new SystemIndicatorDto(SystemIndicatorState.Ok, "qdrant ok"),
            new SystemIndicatorDto(SystemIndicatorState.Ok, "auth ok")));
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/dashboard/system-status");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(SystemStatusDto status)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<ISystemStatusReader>(new StubSystemStatusReader(status));
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

    private static async Task<SystemStatusDto> ReadAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SystemStatusDto>(content, SerializerOptions)!;
    }

    private sealed class StubSystemStatusReader : ISystemStatusReader
    {
        private readonly SystemStatusDto _status;

        public StubSystemStatusReader(SystemStatusDto status)
        {
            _status = status;
        }

        public Task<SystemStatusDto> GetStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(_status);
    }
}
