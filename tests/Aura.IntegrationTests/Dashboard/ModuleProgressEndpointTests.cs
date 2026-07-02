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

public class ModuleProgressEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public ModuleProgressEndpointTests(WebApplicationFactory<ApiMarker> factory)
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
    public async Task GetModuleProgress_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/module-progress");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetModuleProgress_WithToken_Returns200Payload()
    {
        var client = CreateAuthenticatedClient(new ModuleProgressDto(
            [
                new ModuleEntryDto("module-a", ModuleProgressState.Pending),
                new ModuleEntryDto("module-b", ModuleProgressState.InProgress),
                new ModuleEntryDto("module-c", ModuleProgressState.Completed)
            ],
            IsSeeded: true));

        var response = await client.GetAsync("/api/dashboard/module-progress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ReadAsync(response);

        Assert.True(payload.IsSeeded);
        Assert.Equal(3, payload.Entries.Count);
        Assert.Collection(
            payload.Entries,
            entry =>
            {
                Assert.Equal("module-a", entry.ModuleId);
                Assert.Equal(ModuleProgressState.Pending, entry.State);
            },
            entry =>
            {
                Assert.Equal("module-b", entry.ModuleId);
                Assert.Equal(ModuleProgressState.InProgress, entry.State);
            },
            entry =>
            {
                Assert.Equal("module-c", entry.ModuleId);
                Assert.Equal(ModuleProgressState.Completed, entry.State);
            });
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task WriteVerbs_AreRejectedWith405(string method)
    {
        var client = CreateAuthenticatedClient(new ModuleProgressDto([], IsSeeded: true));
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/dashboard/module-progress");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(ModuleProgressDto payload)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IModuleProgressReader>(new StubModuleProgressReader(payload));
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

    private static async Task<ModuleProgressDto> ReadAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ModuleProgressDto>(content, SerializerOptions)!;
    }

    private sealed class StubModuleProgressReader : IModuleProgressReader
    {
        private readonly ModuleProgressDto _payload;

        public StubModuleProgressReader(ModuleProgressDto payload)
        {
            _payload = payload;
        }

        public Task<ModuleProgressDto> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(_payload);
    }
}
