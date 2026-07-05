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

public class DashboardPriorityEndpointTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public DashboardPriorityEndpointTests(WebApplicationFactory<ApiMarker> factory)
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
    // Dashboard Preview — priority fields
    // ============================================================

    [Fact]
    public async Task GetDashboardPreview_ReturnsPriorityCounts()
    {
        var preview = new DashboardPreviewDto(
            [
                new InboxSourceGroupDto("outlook", [new InboxItemPreviewDto("Email 1", "outlook", "1h ago", 85, "Review and reply") { PriorityScore = 85 }]),
                new InboxSourceGroupDto("teams", [new InboxItemPreviewDto("Chat 1", "teams", "5m ago", 60, "Review and respond") { PriorityScore = 45 }]),
            ],
            [new SummaryPreviewEntryDto(1, "Email 1", "outlook", 85)]);

        var client = CreateAuthenticatedClient(new StubDashboardPreviewReader(preview));

        var response = await client.GetAsync("/api/dashboard/preview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("totalPendingCount", out var totalPending));
        Assert.Equal(2, totalPending.GetInt32());

        Assert.True(root.TryGetProperty("highPriorityCount", out var highPriority));
        Assert.Equal(1, highPriority.GetInt32());
    }

    [Fact]
    public async Task GetDashboardPreview_HighPriorityUses75ThresholdAndCriticalDefault()
    {
        var preview = new DashboardPreviewDto(
            [
                new InboxSourceGroupDto("outlook",
                [
                    new InboxItemPreviewDto("Critical null", "outlook", "1h ago", 0.95, "Review and reply")
                    {
                        PriorityHint = "Critical",
                        PriorityScore = null,
                        CapturedAtUtc = new DateTimeOffset(2026, 7, 5, 10, 0, 0, TimeSpan.Zero)
                    },
                    new InboxItemPreviewDto("74 should not count", "outlook", "2h ago", 0.74, "Review and reply")
                    {
                        PriorityHint = "High",
                        PriorityScore = 74,
                        CapturedAtUtc = new DateTimeOffset(2026, 7, 5, 9, 0, 0, TimeSpan.Zero)
                    },
                    new InboxItemPreviewDto("75 should count", "outlook", "3h ago", 0.75, "Review and reply")
                    {
                        PriorityHint = "High",
                        PriorityScore = 75,
                        CapturedAtUtc = new DateTimeOffset(2026, 7, 5, 8, 0, 0, TimeSpan.Zero)
                    }
                ])
            ],
            [new SummaryPreviewEntryDto(1, "Critical null", "outlook", 0.95)]);

        var client = CreateAuthenticatedClient(new StubDashboardPreviewReader(preview));

        var response = await client.GetAsync("/api/dashboard/preview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.Equal(3, root.GetProperty("totalPendingCount").GetInt32());
        Assert.Equal(2, root.GetProperty("highPriorityCount").GetInt32());
    }

    [Fact]
    public async Task GetDashboardPreview_TieAtTopBoundaryIncludesAllTiedItemsAndUsesRecency()
    {
        var preview = new DashboardPreviewDto(
            [
                new InboxSourceGroupDto("outlook",
                [
                    new InboxItemPreviewDto("Most recent 95", "outlook", "1m ago", 0.95, "Review and reply")
                    {
                        PriorityScore = 95,
                        PriorityHint = "Critical",
                        CapturedAtUtc = new DateTimeOffset(2026, 7, 5, 12, 5, 0, TimeSpan.Zero)
                    },
                    new InboxItemPreviewDto("90", "outlook", "2m ago", 0.90, "Review and reply")
                    {
                        PriorityScore = 90,
                        PriorityHint = "High",
                        CapturedAtUtc = new DateTimeOffset(2026, 7, 5, 12, 4, 0, TimeSpan.Zero)
                    },
                    new InboxItemPreviewDto("95 older", "outlook", "3m ago", 0.95, "Review and reply")
                    {
                        PriorityScore = 95,
                        PriorityHint = "Critical",
                        CapturedAtUtc = new DateTimeOffset(2026, 7, 5, 12, 3, 0, TimeSpan.Zero)
                    },
                    new InboxItemPreviewDto("90 tied boundary", "outlook", "4m ago", 0.90, "Review and reply")
                    {
                        PriorityScore = 90,
                        PriorityHint = "High",
                        CapturedAtUtc = new DateTimeOffset(2026, 7, 5, 12, 2, 0, TimeSpan.Zero)
                    },
                    new InboxItemPreviewDto("70", "outlook", "5m ago", 0.70, "Review and reply")
                    {
                        PriorityScore = 70,
                        PriorityHint = "Medium",
                        CapturedAtUtc = new DateTimeOffset(2026, 7, 5, 12, 1, 0, TimeSpan.Zero)
                    }
                ])
            ],
            [new SummaryPreviewEntryDto(1, "Most recent 95", "outlook", 0.95)]);

        var client = CreateAuthenticatedClient(new StubDashboardPreviewReader(preview));

        var response = await client.GetAsync("/api/dashboard/preview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var topItems = json.RootElement.GetProperty("topItems");

        Assert.Equal(4, topItems.GetArrayLength());
        Assert.Equal("Most recent 95", topItems[0].GetProperty("title").GetString());
        Assert.Equal("95 older", topItems[1].GetProperty("title").GetString());
        Assert.Equal("90", topItems[2].GetProperty("title").GetString());
        Assert.Equal("90 tied boundary", topItems[3].GetProperty("title").GetString());
    }

    [Fact]
    public async Task GetDashboardPreview_WithPriorityData_ReturnsTopItems()
    {
        var preview = new DashboardPreviewDto(
            [
                new InboxSourceGroupDto("outlook", [
                    new InboxItemPreviewDto("Email A", "outlook", "1h ago", 90, "Review and reply") { PriorityScore = 90 },
                    new InboxItemPreviewDto("Email B", "outlook", "2h ago", 80, "Review and reply") { PriorityScore = 80 },
                    new InboxItemPreviewDto("Email C", "outlook", "3h ago", 70, "Review and reply") { PriorityScore = 70 },
                ]),
            ],
            [new SummaryPreviewEntryDto(1, "Email A", "outlook", 90)]);

        var client = CreateAuthenticatedClient(new StubDashboardPreviewReader(preview));

        var response = await client.GetAsync("/api/dashboard/preview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("topItems", out var topItems));
        Assert.Equal(3, topItems.GetArrayLength());
    }

    // ============================================================
    // Dashboard Initial — priority fields
    // ============================================================

    [Fact]
    public async Task GetDashboardInitial_ReturnsPriorityCountFields()
    {
        var dashboard = new InitialDashboardDto("Test User", [new DashboardCardDto("Signed in as", "Test User", "info")]);

        var client = CreateAuthenticatedClientInitial(new StubInitialDashboardReader(dashboard));

        var response = await client.GetAsync("/api/dashboard/initial");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("totalPendingCount", out var totalPending));
        Assert.True(root.TryGetProperty("highPriorityCount", out var highPriority));
        Assert.True(root.TryGetProperty("topItems", out var topItems));
        Assert.Equal(0, totalPending.GetInt32());
        Assert.Equal(0, highPriority.GetInt32());
        Assert.Equal(0, topItems.GetArrayLength());
    }

    // ============================================================
    // Helpers
    // ============================================================

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

    private HttpClient CreateAuthenticatedClientInitial(IInitialDashboardReader reader)
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

    private sealed class StubInitialDashboardReader : IInitialDashboardReader
    {
        private readonly InitialDashboardDto _dashboard;

        public StubInitialDashboardReader(InitialDashboardDto dashboard) => _dashboard = dashboard;

        public Task<InitialDashboardDto> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(_dashboard);
    }

    private sealed class StubDashboardPreviewReader : IDashboardPreviewReader
    {
        private readonly DashboardPreviewDto _preview;

        public StubDashboardPreviewReader(DashboardPreviewDto preview) => _preview = preview;

        public Task<DashboardPreviewDto> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(_preview);
    }
}
