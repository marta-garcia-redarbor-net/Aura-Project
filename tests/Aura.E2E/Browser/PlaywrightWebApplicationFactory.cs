using Aura.Application.Ports;
using Aura.Application.UseCases.Calendar;
using Aura.Domain.Calendar;
using Aura.UI.Components;
using Aura.UI.Models;
using Aura.UI.Services;
using Aura.E2E.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace Aura.E2E.Browser;

/// <summary>
/// Creates and manages a real Kestrel-hosted instance of Aura.UI for browser-based testing.
/// Unlike <c>WebApplicationFactory</c> (which uses in-memory TestServer), this factory starts
/// a real TCP listener that Playwright's external Chromium process can navigate to.
/// </summary>
/// <remarks>
/// Architecture Decision: We intentionally bypass <c>WebApplicationFactory</c> because .NET 9's
/// minimal hosting pattern forces TestServer as the transport even when Kestrel URLs are configured.
/// This standalone approach mirrors <c>Program.cs</c> setup with stubbed services, guaranteeing a
/// real HTTP endpoint. Port 0 is used so the OS assigns a free port, preventing TCP TIME_WAIT
/// conflicts on consecutive test runs.
///
/// All external service dependencies are replaced with deterministic stubs to ensure isolated,
/// repeatable test execution.
/// </remarks>
public sealed class PlaywrightWebApplicationFactory : IAsyncDisposable
{
    private WebApplication? _app;

    /// <summary>
    /// The base URL where Kestrel is listening. Set after <see cref="StartAsync"/> discovers
    /// the OS-assigned port. Uses explicit IPv4 loopback to avoid
    /// IPv6 resolution issues with Playwright's Chromium.
    /// </summary>
    public string BaseUrl { get; private set; } = "http://127.0.0.1:0";

    /// <summary>
    /// Builds and starts the real Kestrel server with all services stubbed.
    /// Must be called before navigating with Playwright.
    /// </summary>
    public async Task StartAsync()
    {
        // Resolve the Aura.UI project's content root so static files and Razor components are found
        var uiProjectDir = FindUiProjectDirectory();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = uiProjectDir,
            WebRootPath = Path.Combine(uiProjectDir, "wwwroot"),
            EnvironmentName = "Development"
        });

        builder.WebHost.UseUrls("http://127.0.0.1:0");

        // Mirror Program.cs service registration
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddAuthenticatedUiTestUser();

        // Stub all external API clients with deterministic responses
        builder.Services.AddScoped<IDashboardApiClient>(_ =>
            new StubDelayedDashboardApiClient(
                TimeSpan.FromMilliseconds(150),
                new InitialDashboardResponse(
                    "Test User",
                    [new DashboardCardResponse("Inbox", "7 pending", "info")])));

        builder.Services.AddScoped<ISystemStatusApiClient>(_ =>
            new StubSystemStatusApiClient(new SystemStatusResponse(
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "API healthy"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "Qdrant operational"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "Auth active"))));

        builder.Services.AddScoped<IModuleProgressApiClient>(_ =>
            new StubModuleProgressApiClient(new ModuleProgressResponse(
                [
                    new ModuleEntryResponse("w1-ingestion", ModuleProgressStateResponse.InProgress),
                    new ModuleEntryResponse("w1-triage", ModuleProgressStateResponse.Pending)
                ],
                IsSeeded: true)));

        builder.Services.AddScoped<IDashboardPreviewApiClient>(_ =>
            new StubDashboardPreviewApiClient(new DashboardPreviewResponse(
                [
                    new InboxSourceGroupResponse("outlook",
                        [new InboxItemPreviewResponse("PR review", "outlook", "2h ago", 91.4, "Review and triage")])
                ],
                [new SummaryPreviewEntryResponse(1, "PR review", "outlook", 91.4)])));

        builder.Services.AddScoped<IGraphConnectorApiClient>(_ =>
            new StubGraphConnectorApiClient(
                new GraphConnectorStatusResponse("connected")));

        builder.Services.AddScoped<ISyncApiClient>(_ =>
            new StubSyncApiClient());

        builder.Services.AddScoped<IFocusStateApiClient>(_ =>
            new StubFocusStateApiClient(new FocusStateResponse(
                State: "WindowOfOpportunity",
                IsOverridden: false,
                UserId: "system")));
        builder.Services.AddSingleton<IFocusStateRefreshScheduler, NoopFocusStateRefreshScheduler>();

        builder.Services.AddScoped<ITokenAcquisitionService, DevTokenAcquisitionService>();

        // Calendar use case — dashboard display only
        builder.Services.AddSingleton<ICalendarEventStore, InMemoryCalendarEventStore>();
        builder.Services.AddScoped<GetUpcomingMeetingsUseCase>();

        _app = builder.Build();

        // Mirror Program.cs middleware pipeline (skip HTTPS redirect in test)
        _app.UseStaticFiles();
        _app.UseAntiforgery();
        _app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        await _app.StartAsync();

        // Discover the OS-assigned port from the bound server address
        var addresses = _app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses;
        var boundAddress = addresses.First();
        var boundPort = boundAddress.Split(':').Last();
        BaseUrl = $"http://127.0.0.1:{boundPort}";

        await EnsureHostReachableAsync(BaseUrl);
    }

    /// <summary>
    /// Stops and disposes the Kestrel server.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    #region Stub Implementations

    /// <summary>
    /// Walks up from the test assembly directory to find the Aura.UI source project,
    /// which contains wwwroot, Components, and Razor files needed at runtime.
    /// </summary>
    private static string FindUiProjectDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Aura.UI");
            if (Directory.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not find src/Aura.UI directory. Ensure tests run from the repository root.");
    }

    private sealed class StubDelayedDashboardApiClient(
        TimeSpan delay,
        InitialDashboardResponse response) : IDashboardApiClient
    {
        public async Task<InitialDashboardResponse> GetInitialDashboardAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return response;
        }
    }

    private sealed class StubSystemStatusApiClient(SystemStatusResponse response) : ISystemStatusApiClient
    {
        public Task<SystemStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(response);

        public Task<List<ErrorEntryDto>> GetRecentErrorsAsync(CancellationToken cancellationToken)
            => Task.FromResult(new List<ErrorEntryDto>());
    }

    private sealed class StubModuleProgressApiClient(ModuleProgressResponse response) : IModuleProgressApiClient
    {
        public Task<ModuleProgressResponse> GetAsync(CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class StubDashboardPreviewApiClient(DashboardPreviewResponse response) : IDashboardPreviewApiClient
    {
        public Task<DashboardPreviewResponse> GetPreviewAsync(CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class StubGraphConnectorApiClient(GraphConnectorStatusResponse response) : IGraphConnectorApiClient
    {
        public Task<GraphConnectorStatusResponse> GetStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class StubSyncApiClient : ISyncApiClient
    {
        public Task<List<SourceSyncStateDto>> GetSyncStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(new List<SourceSyncStateDto>
            {
                new() { Source = "outlook", Status = "success", ItemCount = 5, LastSyncTimestamp = DateTimeOffset.UtcNow },
                new() { Source = "teams", Status = "success", ItemCount = 3, LastSyncTimestamp = DateTimeOffset.UtcNow }
            });

        public Task TriggerSyncAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class StubFocusStateApiClient(FocusStateResponse response) : IFocusStateApiClient
    {
        public Task<FocusStateResponse> GetCurrentAsync(CancellationToken cancellationToken)
            => Task.FromResult(response);

        public Task SetOverrideAsync(string state, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ClearOverrideAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoopFocusStateRefreshScheduler : IFocusStateRefreshScheduler
    {
        public IDisposable StartRecurring(TimeSpan interval, Func<Task> callback)
            => EmptyDisposable.Instance;

        private sealed class EmptyDisposable : IDisposable
        {
            public static readonly EmptyDisposable Instance = new();
            public void Dispose() { }
        }
    }

    internal static Task EnsureHostReachableAsync(string baseUrl, HttpMessageHandler? httpMessageHandler = null)
    {
        return EnsureHostReachableCoreAsync(baseUrl, httpMessageHandler);
    }

    private static async Task EnsureHostReachableCoreAsync(string baseUrl, HttpMessageHandler? httpMessageHandler)
    {
        var healthUrl = $"{baseUrl}/health";

        try
        {
            using var probeClient = httpMessageHandler is null
                ? new HttpClient()
                : new HttpClient(httpMessageHandler, disposeHandler: false);

            probeClient.Timeout = TimeSpan.FromSeconds(5);

            var response = await probeClient.GetAsync(healthUrl);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"HostNotReachable: {baseUrl} — GET {healthUrl} returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"HostNotReachable: {baseUrl} — {ex.Message}",
                ex);
        }
    }

    #endregion
}
