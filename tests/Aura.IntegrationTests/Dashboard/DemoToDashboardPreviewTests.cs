using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aura.Api;
using Aura.Application.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aura.IntegrationTests.Dashboard;

public class DemoToDashboardPreviewTests : IClassFixture<WebApplicationFactory<ApiMarker>>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly WebApplicationFactory<ApiMarker> _factory;

    public DemoToDashboardPreviewTests(WebApplicationFactory<ApiMarker> factory)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var auraDb = Path.Combine(Path.GetTempPath(), $"aura-demo-preview-ef-{suffix}.db");
        var auraInfraDb = Path.Combine(Path.GetTempPath(), $"aura-demo-preview-infra-{suffix}.db");
        var tokenDb = Path.Combine(Path.GetTempPath(), $"aura-demo-preview-token-{suffix}.db");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("EmbeddingProvider:Endpoint", "https://test.openai.azure.com");
            builder.UseSetting("EmbeddingProvider:DeploymentName", "test-model");
            builder.UseSetting("EmbeddingProvider:ApiKey", "fake-key");
            builder.UseSetting("UseEntraId", "false");
            builder.UseSetting("MockJwt:Key", "aura-test-key-for-integration-tests-minimum-32-characters!");
            builder.UseSetting("DemoMode:Enabled", "true");
            builder.UseSetting("SeedData:Enabled", "false");
            builder.UseSetting("Persistence:Provider", "EntityFramework");
            builder.UseSetting("ConnectionStrings:AuraDb", $"Data Source={auraDb}");
            builder.UseSetting("ConnectionStrings:Aura", $"Data Source={auraInfraDb}");
            builder.UseSetting("ConnectionStrings:TokenCache", $"Data Source={tokenDb}");
        });
    }

    [Fact]
    public async Task PostDemoAll_WithoutAuth_ThenAuthenticatedPreview_ShowsItemsForDashboard()
    {
        var anonymousClient = _factory.CreateClient();
        var authenticatedClient = await CreateAuthenticatedClientAsync();

        var before = await GetPreviewAsync(authenticatedClient);
        Assert.Equal(0, before.TotalPendingCount);
        Assert.Empty(before.InboxGroups);

        var loadResponse = await authenticatedClient.PostAsync("/api/demo/all", null);
        Assert.Equal(HttpStatusCode.OK, loadResponse.StatusCode);

        var after = await GetPreviewAsync(authenticatedClient);

        Assert.Equal(10, after.TotalPendingCount);
        Assert.Equal(5, after.HighPriorityCount);

        var sources = after.InboxGroups.Select(g => g.Source).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("inbox", sources);
        Assert.Contains("messages", sources);
        Assert.Contains("pr", sources);

        Assert.Contains(after.InboxGroups.SelectMany(g => g.Items), i => i.Source == "inbox" && i.Sender is not null);
        Assert.Contains(after.InboxGroups.SelectMany(g => g.Items), i => i.Source == "messages" && i.Snippet is not null);
    }

    [Fact]
    public async Task StartSimulation_WithAuthenticatedUser_EventuallyAppearsInDashboardPreview()
    {
        var client = await CreateAuthenticatedClientAsync();

        var startResponse = await client.PostAsync("/api/demo/start-simulation", null);
        Assert.Equal(HttpStatusCode.OK, startResponse.StatusCode);

        DashboardPreviewDto? preview = null;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            await Task.Delay(1000);
            preview = await GetPreviewAsync(client);
            if (preview.TotalPendingCount > 0)
            {
                break;
            }
        }

        Assert.NotNull(preview);
        Assert.True(preview!.TotalPendingCount > 0, "Simulation should create at least one visible pending item within a few seconds.");
        Assert.Contains(preview.InboxGroups, g => g.Source.Equals("inbox", StringComparison.OrdinalIgnoreCase));
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
        loginResponse.EnsureSuccessStatusCode();
        var content = await loginResponse.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(content);
        return json.RootElement.GetProperty("token").GetString()!;
    }

    private static async Task<DashboardPreviewDto> GetPreviewAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/dashboard/preview");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DashboardPreviewDto>(content, SerializerOptions)!;
    }
}
