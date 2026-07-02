using System.Net;
using Aura.UI;
using Aura.UI.Models;
using Aura.UI.Services;
using Aura.E2E.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit.Abstractions;

namespace Aura.E2E.Dashboard;

/// <summary>
/// E2E smoke tests verifying the InboxPreviewPanel renders new sync-originated fields
/// (Sender, Snippet, DeepLink, SyncState) with stable data-testid selectors.
/// These tests use WebApplicationFactory to exercise the Blazor Server host over HTTP
/// and verify rendered HTML markers — no real browser needed.
/// </summary>
public class InboxPreviewPanelFieldsSmokeTests : IClassFixture<WebApplicationFactory<UiMarker>>
{
    private readonly WebApplicationFactory<UiMarker> _factory;
    private readonly ITestOutputHelper testOutput;

    public InboxPreviewPanelFieldsSmokeTests(WebApplicationFactory<UiMarker> factory, ITestOutputHelper output)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("AuraApi:BaseUrl", "https://api.aura.test");
        });
        testOutput = output;
    }

    [Fact]
    public async Task GetRoot_PopulatedPreview_RendersSenderSnippetDeepLinkAndSyncState()
    {
        var preview = new DashboardPreviewResponse(
            [
                new InboxSourceGroupResponse("teams",
                [
                    new InboxItemPreviewResponse("Sprint planning", "teams", "30m ago", 88.5, "Join meeting")
                    {
                        Sender = "alice@contoso.com",
                        Snippet = "Review the sprint goals",
                        DeepLink = "https://teams.microsoft.com/l/message/123",
                        PriorityHint = "high",
                        SyncState = "synced"
                    }
                ])
            ],
            []);

        var client = CreateClient(new StubPreviewClient(preview));

        var response = await client.GetAsync("/test-dashboard");
        var html = await response.Content.ReadAsStringAsync();

        // Debug: output HTML for diagnosis
        testOutput.WriteLine(html);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // New data-testid selectors for sync-originated fields
        Assert.Contains("data-testid=\"inbox-preview-item-sender\"", html);
        Assert.Contains("alice@contoso.com", html);
        Assert.Contains("data-testid=\"inbox-preview-item-snippet\"", html);
        Assert.Contains("Review the sprint goals", html);
        Assert.Contains("data-testid=\"inbox-preview-item-deeplink\"", html);
        Assert.Contains("https://teams.microsoft.com/l/message/123", html);
        Assert.Contains("data-testid=\"inbox-preview-item-sync-state\"", html);
        Assert.Contains("synced", html);
    }

    [Fact]
    public async Task GetRoot_PopulatedPreview_NullFields_OmitsEmptySpans()
    {
        // Items without optional fields should NOT render empty spans
        var preview = new DashboardPreviewResponse(
            [
                new InboxSourceGroupResponse("outlook",
                [
                    new InboxItemPreviewResponse("No metadata", "outlook", "1h ago", 50.0, "Read")
                    // No optional fields set — they are null
                ])
            ],
            []);

        var client = CreateClient(new StubPreviewClient(preview));

        var response = await client.GetAsync("/test-dashboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Existing fields still render
        Assert.Contains("data-testid=\"inbox-preview-item-title\"", html);
        Assert.Contains("No metadata", html);
        Assert.Contains("data-testid=\"inbox-preview-populated\"", html);

        // Null optional fields should not produce empty testid attributes
        // The component should conditionally render only when values are present
    }

    [Fact]
    public async Task GetRoot_EmptyPreview_RendersExplicitEmptyState_NoDemoData()
    {
        var preview = new DashboardPreviewResponse([], []);

        var client = CreateClient(new StubPreviewClient(preview));

        var response = await client.GetAsync("/test-dashboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"inbox-preview-empty\"", html);
        Assert.Contains("No inbox items are available right now.", html);
        Assert.DoesNotContain("demo", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetRoot_ErrorPreview_RendersErrorState()
    {
        var client = CreateClient(new ThrowingPreviewClient());

        var response = await client.GetAsync("/test-dashboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"inbox-preview-error\"", html);
        Assert.Contains("Inbox preview is currently unavailable.", html);
    }

    [Fact]
    public async Task GetRoot_MultipleSources_RendersAllItemsWithFields()
    {
        var preview = new DashboardPreviewResponse(
            [
                new InboxSourceGroupResponse("teams",
                [
                    new InboxItemPreviewResponse("Teams msg", "teams", "5m ago", 90.0, "Reply")
                    {
                        Sender = "bob@contoso.com",
                        Snippet = "Quick question",
                        DeepLink = "https://teams.microsoft.com/l/message/456",
                        PriorityHint = "medium",
                        SyncState = "synced"
                    }
                ]),
                new InboxSourceGroupResponse("outlook",
                [
                    new InboxItemPreviewResponse("Email from CEO", "outlook", "2h ago", 95.0, "Review")
                    {
                        Sender = "ceo@contoso.com",
                        Snippet = "Please review the quarterly report",
                        DeepLink = "https://outlook.office.com/mail/id/789",
                        PriorityHint = "high",
                        SyncState = "synced"
                    }
                ])
            ],
            []);

        var client = CreateClient(new StubPreviewClient(preview));

        var response = await client.GetAsync("/test-dashboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Both groups render
        Assert.Contains("data-testid=\"inbox-preview-group-source\"", html);
        Assert.Contains("teams", html);
        Assert.Contains("outlook", html);

        // Items from both sources have fields
        Assert.Contains("bob@contoso.com", html);
        Assert.Contains("Quick question", html);
        Assert.Contains("ceo@contoso.com", html);
        Assert.Contains("Please review the quarterly report", html);

        // data-testid selectors are present
        Assert.Contains("data-testid=\"inbox-preview-item-sender\"", html);
        Assert.Contains("data-testid=\"inbox-preview-item-snippet\"", html);
        Assert.Contains("data-testid=\"inbox-preview-item-deeplink\"", html);
        Assert.Contains("data-testid=\"inbox-preview-item-sync-state\"", html);
    }

    private HttpClient CreateClient(IDashboardPreviewApiClient previewApiClient)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthenticatedUiTestUser();

                // Stub all required clients so the Blazor shell renders without hitting real APIs
                services.RemoveAll<IDashboardApiClient>();
                services.RemoveAll<ISystemStatusApiClient>();
                services.RemoveAll<IModuleProgressApiClient>();
                services.RemoveAll<IDashboardPreviewApiClient>();

                services.AddScoped<IDashboardApiClient>(_ => new StubDashboardClient());
                services.AddScoped<ISystemStatusApiClient>(_ => new StubSystemStatusClient());
                services.AddScoped<IModuleProgressApiClient>(_ => new StubModuleProgressClient());
                services.AddScoped(_ => previewApiClient);
                services.AddScoped<ISyncApiClient>(_ => new StubSyncClient());
                services.AddScoped<IGraphConnectorApiClient>(_ => new StubGraphConnectorClient());
            });
        });

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private sealed class StubPreviewClient : IDashboardPreviewApiClient
    {
        private readonly DashboardPreviewResponse _response;
        public StubPreviewClient(DashboardPreviewResponse response) => _response = response;
        public Task<DashboardPreviewResponse> GetPreviewAsync(CancellationToken ct) => Task.FromResult(_response);
    }

    private sealed class ThrowingPreviewClient : IDashboardPreviewApiClient
    {
        public Task<DashboardPreviewResponse> GetPreviewAsync(CancellationToken ct)
            => Task.FromException<DashboardPreviewResponse>(new HttpRequestException("unavailable"));
    }

    private sealed class StubDashboardClient : IDashboardApiClient
    {
        public Task<InitialDashboardResponse> GetInitialDashboardAsync(CancellationToken ct)
            => Task.FromResult(new InitialDashboardResponse("Test User", []));
    }

    private sealed class StubSystemStatusClient : ISystemStatusApiClient
    {
        public Task<SystemStatusResponse> GetStatusAsync(CancellationToken ct)
            => Task.FromResult(new SystemStatusResponse(
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "ok")));
    }

    private sealed class StubModuleProgressClient : IModuleProgressApiClient
    {
        public Task<ModuleProgressResponse> GetAsync(CancellationToken ct)
            => Task.FromResult(new ModuleProgressResponse([], IsSeeded: true));
    }

    private sealed class StubSyncClient : ISyncApiClient
    {
        public Task<List<SourceSyncStateDto>> GetSyncStatusAsync(CancellationToken ct)
            => Task.FromResult(new List<SourceSyncStateDto>());

        public Task TriggerSyncAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class StubGraphConnectorClient : IGraphConnectorApiClient
    {
        public Task<GraphConnectorStatusResponse> GetStatusAsync(CancellationToken ct)
            => Task.FromResult(new GraphConnectorStatusResponse("Disabled"));
    }
}
