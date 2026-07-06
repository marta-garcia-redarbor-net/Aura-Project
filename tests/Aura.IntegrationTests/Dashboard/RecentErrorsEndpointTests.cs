using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aura.Api;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.IntegrationTests.Dashboard;

public class RecentErrorsEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public RecentErrorsEndpointTests(WebApplicationFactory<ApiMarker> factory)
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
    public async Task GetRecentErrors_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/recent-errors");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRecentErrors_WithTokenAndNoErrors_Returns200EmptyList()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/dashboard/recent-errors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("[]", content);
    }

    [Fact]
    public async Task GetRecentErrors_WithSeededErrors_ReturnsErrorEntries()
    {
        var store = new StubErrorStore();
        await store.RecordAsync(new ErrorEntry("corr-1", new DateTimeOffset(2026, 7, 6, 10, 0, 0, TimeSpan.Zero), "Error one"), CancellationToken.None);
        await store.RecordAsync(new ErrorEntry("corr-2", new DateTimeOffset(2026, 7, 6, 10, 1, 0, TimeSpan.Zero), "Error two"), CancellationToken.None);

        var client = CreateAuthenticatedClient(store);

        var response = await client.GetAsync("/api/dashboard/recent-errors");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await ReadAsync(response);

        Assert.Contains(payload, e => e.CorrelationId == "corr-1" && e.Message == "Error one");
        Assert.Contains(payload, e => e.CorrelationId == "corr-2" && e.Message == "Error two");
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task WriteVerbs_AreRejectedWith405(string method)
    {
        var client = CreateAuthenticatedClient();
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/dashboard/recent-errors");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient(IErrorStore? errorStore = null)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                if (errorStore is not null)
                {
                    services.AddSingleton<IErrorStore>(errorStore);
                }
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

    private static async Task<List<ErrorEntryDto>> ReadAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ErrorEntryDto>>(content, SerializerOptions) ?? [];
    }

    private sealed class StubErrorStore : IErrorStore
    {
        private readonly List<ErrorEntry> _entries = [];

        public Task RecordAsync(ErrorEntry entry, CancellationToken ct = default)
        {
            _entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ErrorEntry>> GetRecentAsync(int count, CancellationToken ct = default)
        {
            var result = _entries.OrderByDescending(e => e.Timestamp).Take(count).ToList().AsReadOnly();
            return Task.FromResult<IReadOnlyList<ErrorEntry>>(result);
        }
    }

    // DTO for deserialization matching the ErrorEntry record shape
    private sealed record ErrorEntryDto(string CorrelationId, DateTimeOffset Timestamp, string Message);
}
