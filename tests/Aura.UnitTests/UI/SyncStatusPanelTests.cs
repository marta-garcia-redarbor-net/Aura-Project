using Aura.UI.Components.Dashboard;
using Aura.UI.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.UI;

public class SyncStatusPanelTests : TestContext
{
    [Fact]
    public void SyncStatusPanel_Renders_SyncNowButton()
    {
        // Arrange: register a fake ISyncApiClient
        Services.AddScoped<ISyncApiClient>(_ => new FakeSyncApiClient());

        // Act
        var cut = RenderComponent<SyncStatusPanel>();

        // Assert: sync now button is present
        var button = cut.Find("[data-testid='sync-now-button']");
        Assert.NotNull(button);
        Assert.Contains("Sync now", button.TextContent);
    }

    [Fact]
    public void SyncStatusPanel_InitialLoad_FetchesSyncStatus()
    {
        // Arrange: register a fake ISyncApiClient that returns sync status
        Services.AddScoped<ISyncApiClient>(_ => new FakeSyncApiClient(
            new List<SourceSyncStateDto>
            {
                new() { Source = "outlook", Status = "success", ItemCount = 5, LastSyncTimestamp = DateTimeOffset.Parse("2025-01-15T10:00:00Z") },
                new() { Source = "teams", Status = "success", ItemCount = 3, LastSyncTimestamp = DateTimeOffset.Parse("2025-01-15T10:01:00Z") },
                new() { Source = "calendar", Status = "failure", ItemCount = 0, LastSyncTimestamp = null }
            }));

        // Act
        var cut = RenderComponent<SyncStatusPanel>();

        // Assert: status sources are rendered
        var sourceItems = cut.FindAll("[data-testid^='sync-source-']");
        Assert.Equal(3, sourceItems.Count);
    }

    [Fact]
    public void SyncStatusPanel_HttpClientInjected_CanRender()
    {
        // Arrange: minimal ISyncApiClient registration
        Services.AddScoped<ISyncApiClient>(_ => new FakeSyncApiClient());

        // Act
        var cut = RenderComponent<SyncStatusPanel>();

        // Assert: component renders without error
        Assert.NotNull(cut.Find("[data-testid='sync-status-panel']"));
        Assert.NotNull(cut.Find("[data-testid='sync-now-button']"));
    }

    private class FakeSyncApiClient : ISyncApiClient
    {
        private readonly List<SourceSyncStateDto> _status;

        public FakeSyncApiClient(List<SourceSyncStateDto>? status = null)
        {
            _status = status ?? new List<SourceSyncStateDto>();
        }

        public Task<List<SourceSyncStateDto>> GetSyncStatusAsync(CancellationToken cancellationToken)
            => Task.FromResult(_status);

        public Task TriggerSyncAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
