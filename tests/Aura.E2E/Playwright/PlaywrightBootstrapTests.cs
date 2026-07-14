using Microsoft.Playwright;
using Aura.E2E.Browser;

namespace Aura.E2E.PlaywrightTests;

/// <summary>
/// Playwright bootstrap scaffold for real-data smoke tests.
/// Requires: dotnet build + pwsh bin/Debug/net9.0/playwright.ps1 install
/// Run: dotnet test --filter "Category=Playwright"
/// </summary>
/// <remarks>
/// This is a SCAFFOLD — not a complete test suite. It verifies:
/// 1. Playwright can launch a browser
/// 2. The Aura UI host is reachable at the configured base URL
/// 3. The dashboard shell renders with the expected data-testid markers
///
/// Real-data smoke tests (login → sync → verify items appear) will be built
/// on top of this foundation in a future iteration.
/// </remarks>
public class PlaywrightBootstrapTests : IAsyncLifetime
{
    private PlaywrightWebApplicationFactory? _factory;
    private Microsoft.Playwright.IPlaywright? _playwright;
    private Microsoft.Playwright.IBrowser? _browser;

    public async Task InitializeAsync()
    {
        _factory = new PlaywrightWebApplicationFactory();
        await _factory.StartAsync();

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
            await _browser.CloseAsync();
        if (_playwright is not null)
            _playwright.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    /// <summary>
    /// Verifies Playwright can launch and navigate to the Aura UI dashboard shell.
    /// This is the minimal smoke test proving the Playwright infrastructure works.
    /// </summary>
    [Fact(Skip = "E2E tests require UI refactor — data-testid attributes and auth setup outdated")]
    public async Task DashboardShell_RendersWithExpectedMarkers()
    {
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_factory!.BaseUrl}/test-dashboard");

        // Verify the dashboard shell renders (matches existing E2E data-testid convention)
        var shell = page.Locator("[data-testid='dashboard-shell']");
        await shell.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });

        Assert.True(await shell.IsVisibleAsync());

        // Verify sidebar renders
        var sidebar = page.Locator("[data-testid='dashboard-sidebar']");
        Assert.True(await sidebar.IsVisibleAsync());

        await context.CloseAsync();
    }

    /// <summary>
    /// Verifies the inbox preview panel renders on the dashboard.
    /// This test will be extended to verify real data after sync integration.
    /// </summary>
    [Fact(Skip = "E2E tests require UI refactor — data-testid attributes and auth setup outdated")]
    public async Task InboxPreviewPanel_RendersOnDashboard()
    {
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_factory!.BaseUrl}/test-dashboard");

        var inboxPanel = page.Locator("[data-testid='inbox-preview-panel']");
        await inboxPanel.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });

        Assert.True(await inboxPanel.IsVisibleAsync());

        await context.CloseAsync();
    }

    /// <summary>
    /// Verifies the sync status panel renders on the dashboard.
    /// </summary>
    [Fact(Skip = "E2E tests require UI refactor — data-testid attributes and auth setup outdated")]
    public async Task SyncStatusPanel_RendersOnDashboard()
    {
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_factory!.BaseUrl}/test-dashboard");

        var syncPanel = page.Locator("[data-testid='sync-status-panel']");
        await syncPanel.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });

        Assert.True(await syncPanel.IsVisibleAsync());

        await context.CloseAsync();
    }

    [Fact(Skip = "E2E tests require UI refactor — data-testid attributes and auth setup outdated")]
    public async Task FocusStateBadge_RendersOnDashboard()
    {
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_factory!.BaseUrl}/test-dashboard");

        var panel = page.Locator("[data-testid='focus-state-panel']");
        await panel.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });

        var badge = panel.Locator("[data-testid='focus-state-badge']");
        await badge.WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });

        Assert.True(await badge.IsVisibleAsync());

        await context.CloseAsync();
    }
}
