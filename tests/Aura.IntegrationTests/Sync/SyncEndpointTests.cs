using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aura.Api;
using Aura.Application.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aura.IntegrationTests.Sync;

public class SyncEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public SyncEndpointTests(WebApplicationFactory<ApiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("EmbeddingProvider:Endpoint", "https://test.openai.azure.com");
            builder.UseSetting("EmbeddingProvider:DeploymentName", "test-model");
            builder.UseSetting("EmbeddingProvider:ApiKey", "fake-key");
            builder.UseSetting("GraphConnector:Enabled", "false");
            builder.UseSetting("MockJwt:Key",
                "aura-test-key-for-integration-tests-minimum-32-characters!");
        });
    }

    [Fact]
    public async Task PostSyncNow_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/sync/now", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostSyncNow_WithToken_Returns200WithResults()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/sync/now", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SyncResultDto>(content, SerializerOptions);
        Assert.NotNull(result);
        Assert.True(result!.Results.Count >= 2, "Expected at least teams and outlook results");
    }

    [Fact]
    public async Task PostSyncNow_ThenGetStatus_ReturnsUpdatedState()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Trigger sync
        var syncResponse = await client.PostAsync("/api/sync/now", null);
        Assert.Equal(HttpStatusCode.OK, syncResponse.StatusCode);

        // Get status
        var statusResponse = await client.GetAsync("/api/sync/status");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        var content = await statusResponse.Content.ReadAsStringAsync();
        var states = JsonSerializer.Deserialize<List<SourceSyncState>>(content, SerializerOptions);
        Assert.NotNull(states);
        Assert.True(states!.Count >= 2, "Expected at least 2 source states after sync");
    }

    [Fact]
    public async Task PostSyncNow_ThenGetDashboardPreview_ReturnsItemsWithSyncedFields()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Trigger sync
        var syncResponse = await client.PostAsync("/api/sync/now", null);
        var syncContent = await syncResponse.Content.ReadAsStringAsync();

        // Get dashboard preview
        var previewResponse = await client.GetAsync("/api/dashboard/preview");
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var content = await previewResponse.Content.ReadAsStringAsync();

        // Verify the response contains inbox groups
        Assert.Contains("inboxGroups", content, StringComparison.OrdinalIgnoreCase);

        // Deserialize to check structure and new fields
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("inboxGroups", out var inboxGroups),
            $"Response must contain inboxGroups property. Sync result: {syncContent}");
        Assert.True(inboxGroups.GetArrayLength() >= 1,
            $"At least one inbox group expected after sync. Sync result: {syncContent}. Preview: {content}");

        // Check that preview items carry the new fields (from fixture data the adapter uses)
        var foundPriorityHint = false;
        foreach (var group in inboxGroups.EnumerateArray())
        {
            if (group.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    // PriorityHint should always be present (derived from WorkItemPriority)
                    if (item.TryGetProperty("priorityHint", out var hint) &&
                        hint.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(hint.GetString()))
                    {
                        foundPriorityHint = true;
                    }
                }
            }
        }

        Assert.True(foundPriorityHint, "At least one preview item should carry a priorityHint field after sync");
    }

    [Fact]
    public async Task GetSyncStatus_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/sync/status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await GetMockTokenAsync(client);
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
}
