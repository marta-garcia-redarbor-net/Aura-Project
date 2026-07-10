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

namespace Aura.IntegrationTests.Triage;

public class TriageEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public TriageEndpointTests(WebApplicationFactory<ApiMarker> factory)
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

    // ============================================================
    // Auth guard
    // ============================================================

    [Fact]
    public async Task GetTriageDecisions_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/triage/decisions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ============================================================
    // Empty
    // ============================================================

    [Fact]
    public async Task GetTriageDecisions_NoData_Returns200WithEmptyItems()
    {
        var store = new StubDecisionStore([]);
        var client = CreateAuthenticatedClient(store);

        var response = await client.GetAsync("/api/triage/decisions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadPayloadAsync(response);
        Assert.Empty(body.Items);
        Assert.Equal(0, body.TotalCount);
        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);
    }

    // ============================================================
    // Populated
    // ============================================================

    [Fact]
    public async Task GetTriageDecisions_WithData_ReturnsPaginatedResults()
    {
        var now = DateTimeOffset.UtcNow;
        var records = new List<InterruptionDecisionRecord>
        {
            new(Guid.NewGuid(), "PR Review", "github", "INTERRUPT", 85, "Urgent fix", now, "WindowOfOpportunity",
                [new DecisionContextItem("ctx-1", "Outage context", "ActivityMemory", 0.95)],
                "LLM confirmed urgency", "confirmed"),
            new(Guid.NewGuid(), "Email thread", "outlook", "QUEUE", 50, "Can wait", now.AddMinutes(-5), "DeepWork",
                [], null, "llm-unavailable"),
        };

        var store = new StubDecisionStore(records);
        var client = CreateAuthenticatedClient(store);

        var response = await client.GetAsync("/api/triage/decisions?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadPayloadAsync(response);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal(2, body.TotalCount);
        Assert.Equal(1, body.Page);
        Assert.Equal(20, body.PageSize);

        var first = body.Items[0];
        Assert.True(first.TryGetProperty("retrievedSemanticContext", out _));
        Assert.True(first.TryGetProperty("llmRationale", out _));
        Assert.True(first.TryGetProperty("guardrailOutcome", out _));
    }

    [Fact]
    public async Task GetTriageDecisions_WithPagination_ReturnsRespectedPageSize()
    {
        var now = DateTimeOffset.UtcNow;
        var records = Enumerable.Range(0, 5).Select(i =>
            new InterruptionDecisionRecord(
                Guid.NewGuid(), $"Item {i}", "github", "QUEUE", null, "", now.AddMinutes(-i), "WindowOfOpportunity")
        ).ToList();

        var store = new StubDecisionStore(records);
        var client = CreateAuthenticatedClient(store);

        var response = await client.GetAsync("/api/triage/decisions?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadPayloadAsync(response);
        Assert.Equal(5, body.TotalCount);
        Assert.Equal(1, body.Page);
        Assert.Equal(2, body.PageSize);
    }

    // ============================================================
    // Error handling
    // ============================================================

    [Fact]
    public async Task GetTriageDecisions_WhenStoreThrows_Returns500()
    {
        var client = CreateAuthenticatedClient(
            new ThrowingDecisionStore(new InvalidOperationException("Store failed")));

        var response = await client.GetAsync("/api/triage/decisions");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Triage decisions request failed", content);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private HttpClient CreateAuthenticatedClient(IInterruptionDecisionStore store)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DisableRateLimiting", "true");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(store);
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

    private static async Task<PagedResultDto> ReadPayloadAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PagedResultDto>(content, SerializerOptions)!;
    }

    // ============================================================
    // Test DTO matching the API contract
    // ============================================================

    private sealed record PagedResultDto
    {
        public IReadOnlyList<JsonElement> Items { get; init; } = Array.Empty<JsonElement>();
        public int TotalCount { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
    }

    // ============================================================
    // Stubs
    // ============================================================

    private sealed class StubDecisionStore : IInterruptionDecisionStore
    {
        private readonly IReadOnlyList<InterruptionDecisionRecord> _records;

        public StubDecisionStore(IReadOnlyList<InterruptionDecisionRecord> records)
        {
            _records = records;
        }

        public Task RecordAsync(InterruptionDecisionRecord record, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<PagedResult<InterruptionDecisionRecord>> QueryAsync(
            int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PagedResult<InterruptionDecisionRecord>
            {
                Items = _records,
                TotalCount = _records.Count,
                Page = page,
                PageSize = pageSize
            });
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class ThrowingDecisionStore : IInterruptionDecisionStore
    {
        private readonly Exception _exception;

        public ThrowingDecisionStore(Exception exception) => _exception = exception;

        public Task RecordAsync(InterruptionDecisionRecord record, CancellationToken cancellationToken = default)
            => throw _exception;

        public Task<PagedResult<InterruptionDecisionRecord>> QueryAsync(
            int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromException<PagedResult<InterruptionDecisionRecord>>(_exception);

        public Task ClearAsync(CancellationToken cancellationToken = default)
            => throw _exception;
    }
}
