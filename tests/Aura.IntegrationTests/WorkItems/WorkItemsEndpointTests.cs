using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aura.Api;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.IntegrationTests.WorkItems;

public class WorkItemsEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public WorkItemsEndpointTests(WebApplicationFactory<ApiMarker> factory)
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
    public async Task GetWorkItems_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/workitems?sourceType=TeamsMessage");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ============================================================
    // PriorityScore in response
    // ============================================================

    [Fact]
    public async Task GetWorkItems_WithPriorityScore_ReturnsPriorityScoreInResponse()
    {
        var items = new List<WorkItem>
        {
            new WorkItem(
                "ext-1", "Priority Item", "teams",
                WorkItemSourceType.TeamsMessage, WorkItemPriority.High,
                new Dictionary<string, string>(),
                priorityScore: 85)
        };

        var reader = new StubWorkItemReader(items);
        var client = CreateAuthenticatedClient(reader);
        var response = await client.GetAsync("/api/workitems?sourceType=TeamsMessage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.Equal(1, root.GetArrayLength());

        var first = root[0];
        Assert.True(first.TryGetProperty("priorityScore", out var scoreProp),
            "Response item must include priorityScore field");
        Assert.Equal(85, scoreProp.GetInt32());
    }

    [Fact]
    public async Task GetWorkItems_WithZeroPriorityScore_ReturnsZero()
    {
        var items = new List<WorkItem>
        {
            new WorkItem(
                "ext-3", "Zero Score", "teams",
                WorkItemSourceType.TeamsMessage, WorkItemPriority.Low,
                new Dictionary<string, string>(),
                priorityScore: 0)
        };

        var reader = new StubWorkItemReader(items);
        var client = CreateAuthenticatedClient(reader);
        var response = await client.GetAsync("/api/workitems?sourceType=TeamsMessage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.Equal(1, root.GetArrayLength());
        Assert.Equal(0, root[0].GetProperty("priorityScore").GetInt32());
    }

    [Fact]
    public async Task GetWorkItems_MultipleItems_EachCarriesOwnPriorityScore()
    {
        var items = new List<WorkItem>
        {
            new WorkItem("ext-a", "Item A", "teams",
                WorkItemSourceType.TeamsMessage, WorkItemPriority.High,
                new Dictionary<string, string>(), priorityScore: 90),
            new WorkItem("ext-b", "Item B", "teams",
                WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium,
                new Dictionary<string, string>(), priorityScore: 50),
            new WorkItem("ext-c", "Item C", "teams",
                WorkItemSourceType.TeamsMessage, WorkItemPriority.Low,
                new Dictionary<string, string>(), priorityScore: null),
        };

        var reader = new StubWorkItemReader(items);
        var client = CreateAuthenticatedClient(reader);
        var response = await client.GetAsync("/api/workitems?sourceType=TeamsMessage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.Equal(3, root.GetArrayLength());
        Assert.Equal(90, root[0].GetProperty("priorityScore").GetInt32());
        Assert.Equal(50, root[1].GetProperty("priorityScore").GetInt32());
        Assert.Equal(JsonValueKind.Null, root[2].GetProperty("priorityScore").ValueKind);
    }

    [Fact]
    public async Task GetWorkItems_WithNullPriorityScore_ReturnsNullPriorityScore()
    {
        var items = new List<WorkItem>
        {
            new WorkItem(
                "ext-2", "No Score Item", "teams",
                WorkItemSourceType.TeamsMessage, WorkItemPriority.Low,
                new Dictionary<string, string>(),
                priorityScore: null)
        };

        var reader = new StubWorkItemReader(items);
        var client = CreateAuthenticatedClient(reader);

        var response = await client.GetAsync("/api/workitems?sourceType=TeamsMessage");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.Equal(1, root.GetArrayLength());

        var first = root[0];
        Assert.True(first.TryGetProperty("priorityScore", out var scoreProp),
            "Response item must include priorityScore field even when null");
        Assert.Equal(JsonValueKind.Null, scoreProp.ValueKind);
    }

    // ============================================================
    // Error handling
    // ============================================================

    [Fact]
    public async Task GetWorkItems_InvalidSourceType_Returns400()
    {
        var reader = new StubWorkItemReader([]);
        var client = CreateAuthenticatedClient(reader);

        var response = await client.GetAsync("/api/workitems?sourceType=invalid");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkItems_WhenReaderThrows_Returns500()
    {
        var reader = new ThrowingWorkItemReader(new InvalidOperationException("Reader failed"));
        var client = CreateAuthenticatedClient(reader);

        var response = await client.GetAsync("/api/workitems?sourceType=TeamsMessage");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Work items request failed", content);
    }

    [Fact]
    public async Task GetWorkItems_EqualScoreRecencyOrdering_UsingRealSqliteStore()
    {
        await using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        Aura.Infrastructure.Adapters.WorkItems.SqliteWorkItemStore.InitializeSchema(connection);
        var sqliteStore = new Aura.Infrastructure.Adapters.WorkItems.SqliteWorkItemStore(connection);

        var older = new WorkItem(
            "sql-old-80", "Older score 80", "teams",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.High,
            new Dictionary<string, string>(),
            capturedAtUtc: new DateTimeOffset(2026, 7, 5, 11, 0, 0, TimeSpan.Zero),
            priorityScore: 80);
        var newer = new WorkItem(
            "sql-new-80", "Newer score 80", "teams",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.High,
            new Dictionary<string, string>(),
            capturedAtUtc: new DateTimeOffset(2026, 7, 5, 12, 0, 0, TimeSpan.Zero),
            priorityScore: 80);

        await sqliteStore.SaveAsync(older, CancellationToken.None);
        await sqliteStore.SaveAsync(newer, CancellationToken.None);

        var client = CreateAuthenticatedClient(sqliteStore);

        var response = await client.GetAsync("/api/workitems?sourceType=TeamsMessage");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.Equal("sql-new-80", root[0].GetProperty("externalId").GetString());
        Assert.Equal("sql-old-80", root[1].GetProperty("externalId").GetString());
    }

    // ============================================================
    // Helpers
    // ============================================================

    private HttpClient CreateAuthenticatedClient(IWorkItemReader reader)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(reader);
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

    // ============================================================
    // Stubs
    // ============================================================

    private sealed class StubWorkItemReader : IWorkItemReader
    {
        private readonly IReadOnlyList<WorkItem> _items;

        public StubWorkItemReader(IReadOnlyList<WorkItem> items)
        {
            _items = items;
        }

        public Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
            MorningSummaryQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(_items);

        public Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
            MorningSummaryQuery query, WorkItemStatus? statusFilter, CancellationToken cancellationToken = default)
            => Task.FromResult(_items);

        public Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
            WorkItemSourceType sourceType, MorningSummaryQuery query, WorkItemStatus? statusFilter,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_items);

        public Task<IReadOnlyList<WorkItem>> ReadBySourceAsync(
            WorkItemSourceType sourceType, WorkItemStatus? statusFilter, string? ownerUserId, CancellationToken cancellationToken = default)
            => Task.FromResult(_items);
    }

    private sealed class ThrowingWorkItemReader : IWorkItemReader
    {
        private readonly Exception _exception;

        public ThrowingWorkItemReader(Exception exception)
        {
            _exception = exception;
        }

        public Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
            MorningSummaryQuery query, CancellationToken cancellationToken = default)
            => throw _exception;

        public Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
            MorningSummaryQuery query, WorkItemStatus? statusFilter, CancellationToken cancellationToken = default)
            => throw _exception;

        public Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
            WorkItemSourceType sourceType, MorningSummaryQuery query, WorkItemStatus? statusFilter,
            CancellationToken cancellationToken = default)
            => throw _exception;

        public Task<IReadOnlyList<WorkItem>> ReadBySourceAsync(
            WorkItemSourceType sourceType, WorkItemStatus? statusFilter, string? ownerUserId, CancellationToken cancellationToken = default)
            => throw _exception;
    }
}
