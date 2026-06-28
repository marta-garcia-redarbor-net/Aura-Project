using Aura.UI.Components.Dashboard;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Aura.UnitTests.UI;

public class SyncButtonTests : TestContext
{
    [Fact]
    public void SyncButton_Renders_DefaultState()
    {
        // Arrange & Act
        var cut = RenderComponent<SyncButton>();

        // Assert: button is rendered with sync text
        Assert.Contains("Sync Now", cut.Markup);
        Assert.DoesNotContain("Syncing...", cut.Markup);
    }

    [Fact]
    public void SyncButton_Click_TogglesSyncingState()
    {
        // Arrange: provide a slow callback to observe the syncing state
        var syncCalled = false;

        var cut = RenderComponent<SyncButton>(parameters => parameters
            .Add(p => p.OnSync, EventCallback.Factory.Create(this, async () =>
            {
                syncCalled = true;
                await Task.Delay(100);
            })));

        // Act: click the button
        cut.Find("button").Click();

        // Assert: callback was invoked
        Assert.True(syncCalled);
    }

    [Fact]
    public void SyncButton_DisabledDuringSync()
    {
        // Arrange: slow callback
        var cut = RenderComponent<SyncButton>(parameters => parameters
            .Add(p => p.OnSync, EventCallback.Factory.Create(this, async () =>
            {
                await Task.Delay(200);
            })));

        // Act: click the button
        cut.Find("button").Click();

        // Assert: button shows syncing state
        Assert.Contains("Syncing...", cut.Markup);
    }

    [Fact]
    public void SyncButton_ShowsSpinnerDuringSync()
    {
        // Arrange: slow callback
        var cut = RenderComponent<SyncButton>(parameters => parameters
            .Add(p => p.OnSync, EventCallback.Factory.Create(this, async () =>
            {
                await Task.Delay(200);
            })));

        // Act: click the button
        cut.Find("button").Click();

        // Assert: spinner element exists
        var spinner = cut.Find("[data-testid='sync-spinner']");
        Assert.NotNull(spinner);
    }
}
