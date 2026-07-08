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

namespace Aura.IntegrationTests.PullRequests;

public class PullRequestsEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public PullRequestsEndpointTests(WebApplicationFactory<ApiMarker> factory)
    {
        var uniqueDb = Path.Combine(Path.GetTempPath(), $"aura-pr-tests-{Guid.NewGuid():N}.db");
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("EmbeddingProvider:Endpoint", "https://test.openai.azure.com");
            builder.UseSetting("EmbeddingProvider:DeploymentName", "test-model");
            builder.UseSetting("EmbeddingProvider:ApiKey", "fake-key");
            builder.UseSetting("UseEntraId", "false");
            builder.UseSetting("MockJwt:Key",
                "aura-test-key-for-integration-tests-minimum-32-characters!");
            builder.UseSetting("ConnectionStrings:Aura", $"Data Source={uniqueDb}");
        });
    }

    // ============================================================
    // Auth guard
    // ============================================================

    [Fact]
    public async Task GetPullRequests_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/pull-requests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ============================================================
    // Owner filter
    // ============================================================

    [Fact]
    public async Task GetPullRequests_NoOwnerFilter_ReturnsAll()
    {
        var items = CreateMixedOwnerItems();
        var reader = new StubWorkItemReader(items);
        var client = CreateAuthenticatedClient(reader);

        var response = await client.GetAsync("/api/pull-requests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        Assert.Equal(6, json.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetPullRequests_WithOwnerFilter_ReturnsMatchingPlusNull()
    {
        var items = CreateMixedOwnerItems();
        var reader = new StubWorkItemReader(items);
        var client = CreateAuthenticatedClient(reader);

        var response = await client.GetAsync("/api/pull-requests?ownerUserId=user-A");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        // 3 owned by user-A + 1 with null owner = 4
        Assert.Equal(4, json.RootElement.GetArrayLength());
    }

    // ============================================================
    // PriorityScore DESC ordering
    // ============================================================

    [Fact]
    public async Task GetPullRequests_OrderedByPriorityScoreDesc()
    {
        var items = new List<WorkItem>
        {
            CreatePrItem("pr-1", priorityScore: 50),
            CreatePrItem("pr-2", priorityScore: 90),
            CreatePrItem("pr-3", priorityScore: 90),
        };
        var reader = new StubWorkItemReader(items);
        var client = CreateAuthenticatedClient(reader);

        var response = await client.GetAsync("/api/pull-requests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.Equal(3, root.GetArrayLength());
        Assert.Equal(90, root[0].GetProperty("priorityScore").GetInt32());
        Assert.Equal(90, root[1].GetProperty("priorityScore").GetInt32());
        Assert.Equal(50, root[2].GetProperty("priorityScore").GetInt32());
    }

    // ============================================================
    // Error handling
    // ============================================================

    [Fact]
    public async Task GetPullRequests_WhenReaderThrows_Returns500()
    {
        var reader = new ThrowingWorkItemReader(new InvalidOperationException("Store failed"));
        var client = CreateAuthenticatedClient(reader);

        var response = await client.GetAsync("/api/pull-requests");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    // ============================================================
    // DTO field mapping
    // ============================================================

    [Fact]
    public async Task GetPullRequests_MapsMetadataFieldsCorrectly()
    {
        var metadata = new Dictionary<string, string>
        {
            ["pr.status"] = "passing",
            ["pr.reviewerCount"] = "2",
            ["pr.commentCount"] = "5",
            ["pr.author"] = "Alice",
            ["pr.repo"] = "Aura"
        };
        var item = new WorkItem(
            externalId: "pr-142",
            title: "Fix: payment crash",
            source: "pr",
            sourceType: WorkItemSourceType.PrReview,
            priority: WorkItemPriority.High,
            metadata: metadata,
            priorityScore: 75);
        var reader = new StubWorkItemReader(new List<WorkItem> { item });
        var client = CreateAuthenticatedClient(reader);

        var response = await client.GetAsync("/api/pull-requests");
        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var first = json.RootElement[0];

        Assert.Equal(142, first.GetProperty("id").GetInt32());
        Assert.Equal("passing", first.GetProperty("status").GetString());
        Assert.Equal(2, first.GetProperty("reviewerCount").GetInt32());
        Assert.Equal(5, first.GetProperty("commentCount").GetInt32());
        Assert.Equal("Alice", first.GetProperty("author").GetString());
        Assert.Equal("Aura", first.GetProperty("repoName").GetString());
        Assert.Equal("Fix: payment crash", first.GetProperty("title").GetString());
        Assert.Equal(75, first.GetProperty("priorityScore").GetInt32());
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static List<WorkItem> CreateMixedOwnerItems()
    {
        return
        [
            CreatePrItem("pr-1", ownerUserId: "user-A", priorityScore: 90),
            CreatePrItem("pr-2", ownerUserId: "user-A", priorityScore: 80),
            CreatePrItem("pr-3", ownerUserId: "user-A", priorityScore: 70),
            CreatePrItem("pr-4", ownerUserId: "user-B", priorityScore: 60),
            CreatePrItem("pr-5", ownerUserId: "user-B", priorityScore: 50),
            CreatePrItem("pr-6", ownerUserId: null, priorityScore: 40),
        ];
    }

    private static WorkItem CreatePrItem(
        string externalId,
        string? ownerUserId = null,
        int? priorityScore = null)
    {
        return new WorkItem(
            externalId: externalId,
            title: $"PR {externalId}",
            source: "pr",
            sourceType: WorkItemSourceType.PrReview,
            priority: WorkItemPriority.Medium,
            metadata: new Dictionary<string, string>(),
            priorityScore: priorityScore,
            ownerUserId: ownerUserId);
    }

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
            WorkItemSourceType sourceType, WorkItemStatus? statusFilter, CancellationToken cancellationToken = default)
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
            WorkItemSourceType sourceType, WorkItemStatus? statusFilter, CancellationToken cancellationToken = default)
            => throw _exception;
    }
}
