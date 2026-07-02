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

public class DashboardPreviewEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public DashboardPreviewEndpointTests(WebApplicationFactory<ApiMarker> factory)
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
    public async Task GetDashboardPreview_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard/preview");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboardPreview_WithTokenAndPopulatedPayload_Returns200WithDashboardShape()
    {
        var preview = new DashboardPreviewDto(
            [
                new InboxSourceGroupDto(
                    "outlook",
                    [
                        new InboxItemPreviewDto("PR review", "outlook", "2h ago", 91.4, "Review and triage")
                    ])
            ],
            [
                new SummaryPreviewEntryDto(1, "PR review", "outlook", 91.4)
            ]);

        var client = CreateAuthenticatedClient(new StubDashboardPreviewReader(preview));

        var response = await client.GetAsync("/api/dashboard/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadPayloadAsync(response);
        var group = Assert.Single(payload.InboxGroups);
        Assert.Equal("outlook", group.Source);
        var item = Assert.Single(group.Items);
        Assert.Equal("PR review", item.Title);
        Assert.Equal("outlook", item.Source);
        Assert.Equal("2h ago", item.RelativeTimestamp);
        Assert.Equal(91.4, item.Score);
        Assert.Equal("Review and triage", item.SuggestedAction);

        var summary = Assert.Single(payload.SummaryEntries);
        Assert.Equal(1, summary.Rank);
        Assert.Equal("PR review", summary.Title);
        Assert.Equal("outlook", summary.Source);
        Assert.Equal(91.4, summary.Score);
    }

    [Fact]
    public async Task GetDashboardPreview_WithTokenAndEmptyPayload_Returns200WithEmptyCollections()
    {
        var client = CreateAuthenticatedClient(new StubDashboardPreviewReader(new DashboardPreviewDto([], [])));

        var response = await client.GetAsync("/api/dashboard/preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadPayloadAsync(response);
        Assert.Empty(payload.InboxGroups);
        Assert.Empty(payload.SummaryEntries);
    }

    [Fact]
    public async Task GetDashboardPreview_WithToken_ResponseContainsOnlyDashboardPreviewFields()
    {
        var preview = new DashboardPreviewDto(
            [
                new InboxSourceGroupDto(
                    "outlook",
                    [
                        new InboxItemPreviewDto("PR review", "outlook", "2h ago", 91.4, "Review and triage")
                    ])
            ],
            [
                new SummaryPreviewEntryDto(1, "PR review", "outlook", 91.4)
            ]);

        var client = CreateAuthenticatedClient(new StubDashboardPreviewReader(preview));

        var response = await client.GetAsync("/api/dashboard/preview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);

        var root = json.RootElement;
        Assert.True(root.TryGetProperty("inboxGroups", out var inboxGroups));
        Assert.True(root.TryGetProperty("summaryEntries", out var summaryEntries));

        Assert.False(root.TryGetProperty("entries", out _));
        Assert.False(root.TryGetProperty("workItems", out _));

        var firstGroup = inboxGroups.EnumerateArray().First();
        Assert.True(firstGroup.TryGetProperty("source", out _));
        Assert.True(firstGroup.TryGetProperty("items", out var items));
        Assert.False(firstGroup.TryGetProperty("metadata", out _));

        var firstItem = items.EnumerateArray().First();
        Assert.True(firstItem.TryGetProperty("title", out _));
        Assert.True(firstItem.TryGetProperty("source", out _));
        Assert.True(firstItem.TryGetProperty("relativeTimestamp", out _));
        Assert.True(firstItem.TryGetProperty("score", out _));
        Assert.True(firstItem.TryGetProperty("suggestedAction", out _));
        Assert.False(firstItem.TryGetProperty("priority", out _));
        Assert.False(firstItem.TryGetProperty("sourceType", out _));
        Assert.False(firstItem.TryGetProperty("status", out _));

        var firstSummary = summaryEntries.EnumerateArray().First();
        Assert.True(firstSummary.TryGetProperty("rank", out _));
        Assert.True(firstSummary.TryGetProperty("title", out _));
        Assert.True(firstSummary.TryGetProperty("source", out _));
        Assert.True(firstSummary.TryGetProperty("score", out _));
        Assert.False(firstSummary.TryGetProperty("item", out _));
    }

    [Fact]
    public async Task GetDashboardPreview_WhenReaderThrows_Returns500Problem()
    {
        var client = CreateAuthenticatedClient(
            new ThrowingDashboardPreviewReader(new InvalidOperationException("Reader exploded")));

        var response = await client.GetAsync("/api/dashboard/preview");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Dashboard preview request failed", content);
    }

    private HttpClient CreateAuthenticatedClient(IDashboardPreviewReader reader)
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

    private static async Task<DashboardPreviewDto> ReadPayloadAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DashboardPreviewDto>(content, SerializerOptions)!;
    }

    private sealed class StubDashboardPreviewReader : IDashboardPreviewReader
    {
        private readonly DashboardPreviewDto _payload;

        public StubDashboardPreviewReader(DashboardPreviewDto payload)
        {
            _payload = payload;
        }

        public Task<DashboardPreviewDto> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(_payload);
    }

    private sealed class ThrowingDashboardPreviewReader : IDashboardPreviewReader
    {
        private readonly Exception _exception;

        public ThrowingDashboardPreviewReader(Exception exception)
        {
            _exception = exception;
        }

        public Task<DashboardPreviewDto> GetAsync(CancellationToken cancellationToken)
            => Task.FromException<DashboardPreviewDto>(_exception);
    }
}
