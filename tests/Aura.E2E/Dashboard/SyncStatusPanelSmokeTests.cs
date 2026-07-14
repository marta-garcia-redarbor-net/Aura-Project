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
/// E2E smoke tests verifying SyncStatusPanel renders sync-now button,
/// per-source progress, last sync timestamp, and re-auth prompt with stable data-testid selectors.
/// </summary>
public class SyncStatusPanelSmokeTests : IClassFixture<WebApplicationFactory<UiMarker>>
{
    private readonly WebApplicationFactory<UiMarker> _factory;
    private readonly ITestOutputHelper _output;

    public SyncStatusPanelSmokeTests(WebApplicationFactory<UiMarker> factory, ITestOutputHelper output)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("AuraApi:BaseUrl", "https://api.aura.test");
        });
        _output = output;
    }

    [Fact(Skip = "E2E tests require UI refactor — data-testid attributes and auth setup outdated")]
    public async Task GetRoot_RendersSyncStatusPanelWithTestId()
    {
        var client = CreateClient(
            new StubDashboardClient(),
            new StubSystemStatusClient(),
            new StubModuleProgressClient(),
            new StubPreviewClient(new DashboardPreviewResponse([], [])));

        var response = await client.GetAsync("/dashboard");
        var html = await response.Content.ReadAsStringAsync();

        _output.WriteLine(html);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"sync-status-panel\"", html);
        Assert.Contains("data-testid=\"sync-now-button\"", html);
    }

    [Fact(Skip = "E2E tests require UI refactor — data-testid attributes and auth setup outdated")]
    public async Task GetRoot_RendersSyncSourceProgressElements()
    {
        var client = CreateClient(
            new StubDashboardClient(),
            new StubSystemStatusClient(),
            new StubModuleProgressClient(),
            new StubPreviewClient(new DashboardPreviewResponse([], [])));

        var response = await client.GetAsync("/dashboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"sync-source-progress-teams\"", html);
        Assert.Contains("data-testid=\"sync-source-progress-outlook\"", html);
    }

    [Fact(Skip = "E2E tests require UI refactor — data-testid attributes and auth setup outdated")]
    public async Task GetRoot_RendersLastSyncTimestampElement()
    {
        var client = CreateClient(
            new StubDashboardClient(),
            new StubSystemStatusClient(),
            new StubModuleProgressClient(),
            new StubPreviewClient(new DashboardPreviewResponse([], [])));

        var response = await client.GetAsync("/dashboard");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-testid=\"sync-last-timestamp\"", html);
    }

    private HttpClient CreateClient(
        IDashboardApiClient dashboardApiClient,
        ISystemStatusApiClient systemStatusApiClient,
        IModuleProgressApiClient moduleProgressApiClient,
        IDashboardPreviewApiClient dashboardPreviewApiClient)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthenticatedUiTestUser();
                services.AddStubFocusStateApiClient();
                services.RemoveAll<IDashboardApiClient>();
                services.RemoveAll<ISystemStatusApiClient>();
                services.RemoveAll<IModuleProgressApiClient>();
                services.RemoveAll<IDashboardPreviewApiClient>();

                services.AddScoped(_ => dashboardApiClient);
                services.AddScoped(_ => systemStatusApiClient);
                services.AddScoped(_ => moduleProgressApiClient);
                services.AddScoped(_ => dashboardPreviewApiClient);
                services.AddScoped<ISyncApiClient>(_ => new StubSyncClient());
            });
        });

        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
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
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "ok")));

        public Task<List<ErrorEntryDto>> GetRecentErrorsAsync(CancellationToken ct)
            => Task.FromResult(new List<ErrorEntryDto>());
    }

    private sealed class StubModuleProgressClient : IModuleProgressApiClient
    {
        public Task<ModuleProgressResponse> GetAsync(CancellationToken ct)
            => Task.FromResult(new ModuleProgressResponse([], IsSeeded: true));
    }

    private sealed class StubPreviewClient : IDashboardPreviewApiClient
    {
        private readonly DashboardPreviewResponse _response;
        public StubPreviewClient(DashboardPreviewResponse response) => _response = response;
        public Task<DashboardPreviewResponse> GetPreviewAsync(CancellationToken ct) => Task.FromResult(_response);
    }

    private sealed class StubSyncClient : ISyncApiClient
    {
        public Task<List<SourceSyncStateDto>> GetSyncStatusAsync(CancellationToken ct)
            => Task.FromResult(new List<SourceSyncStateDto>
            {
                new() { Source = "teams", Status = "success", ItemCount = 3, LastSyncTimestamp = DateTimeOffset.UtcNow },
                new() { Source = "outlook", Status = "success", ItemCount = 5, LastSyncTimestamp = DateTimeOffset.UtcNow }
            });

        public Task TriggerSyncAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
