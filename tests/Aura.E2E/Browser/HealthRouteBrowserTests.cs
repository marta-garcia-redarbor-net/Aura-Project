using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Aura.E2E.Browser;

/// <summary>
/// Browser-based end-to-end test for navigating from the dashboard to the Health page.
/// Validates that clicking the Health sidebar link loads the /health route with all three panels.
/// </summary>
public class HealthRouteBrowserTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private PlaywrightWebApplicationFactory _factory = null!;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private bool _testFailed;

    public HealthRouteBrowserTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _factory = new PlaywrightWebApplicationFactory();
        await _factory.StartAsync();
        _output.WriteLine($"Kestrel server started at {_factory.BaseUrl}");

        var headed = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADED") == "true";
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !headed
        });

        _context = await _browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        _page.SetDefaultTimeout(10_000);
        _page.SetDefaultNavigationTimeout(15_000);

        _output.WriteLine("Playwright browser initialized");
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (_page != null) await _page.CloseAsync();
            if (_context != null) await _context.CloseAsync();
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();
            await _factory.DisposeAsync();
            _output.WriteLine("Cleanup completed");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Cleanup error (non-fatal): {ex.Message}");
        }
    }

    /// <summary>
    /// Navigates to the dashboard root, clicks the Health sidebar link,
    /// and verifies the /health page loads with all three health panels.
    /// </summary>
    [Fact]
    public async Task HealthRoute_SidebarLinkNavigatesToHealthPage_WithPanels()
    {
        Assert.NotNull(_page);

        try
        {
            // Navigate to dashboard root
            _output.WriteLine($"Navigating to {_factory.BaseUrl}/test-dashboard...");
            var response = await _page.GotoAsync($"{_factory.BaseUrl}/test-dashboard", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            Assert.NotNull(response);
            Assert.True(response.Ok, $"Navigation failed with status {response.Status}");

            // Wait for the sidebar to render
            await _page.WaitForSelectorAsync("[data-testid='dashboard-sidebar']");
            _output.WriteLine("Dashboard shell loaded");

            // Click the Health link in the sidebar
            _output.WriteLine("Clicking Health sidebar link...");
            var healthLink = await _page.QuerySelectorAsync("a[href='/health']");
            Assert.NotNull(healthLink);
            await healthLink.ClickAsync();

            // Wait for navigation to /health
            await _page.WaitForURLAsync("**/health", new PageWaitForURLOptions { Timeout = 10_000 });
            _output.WriteLine("Navigated to /health");

            // Verify the three health panels are present
            await _page.WaitForSelectorAsync("[data-testid='graph-connector-panel']");
            _output.WriteLine("graph-connector-panel found");

            var systemStatusPanel = await _page.QuerySelectorAsync("[data-testid='system-status-panel']");
            Assert.NotNull(systemStatusPanel);
            _output.WriteLine("system-status-panel found");

            var moduleProgressPanel = await _page.QuerySelectorAsync("[data-testid='module-progress-panel']");
            Assert.NotNull(moduleProgressPanel);
            _output.WriteLine("module-progress-panel found");

            // Verify page title
            var title = await _page.TextContentAsync("h1");
            Assert.Contains("System Health", title);
            _output.WriteLine("TEST PASSED: Health page loaded with all three panels");
        }
        catch
        {
            _testFailed = true;
            throw;
        }
    }
}
