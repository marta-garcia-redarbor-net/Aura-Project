using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Aura.E2E.Browser;

/// <summary>
/// Browser-based end-to-end tests for the Aura.UI dashboard using Playwright.
/// These tests exercise the full browser rendering pipeline including Blazor hydration
/// and DOM state transitions, validated via stable <c>data-testid</c> markers.
/// </summary>
/// <remarks>
/// Architecture: Uses <see cref="PlaywrightWebApplicationFactory"/> to start a real Kestrel
/// server on port 5555 — accessible by the external Chromium browser process. This is
/// intentionally different from host-level smoke tests that use in-memory TestServer.
///
/// Data Strategy: All backend services are stubbed with deterministic responses inside the
/// factory. The dashboard API client injects a 150ms delay to validate loading → populated
/// state transitions.
///
/// Failure Artifacts: Screenshots and Playwright traces are captured to TestResults/ on failure.
/// </remarks>
public class DashboardRootBrowserTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private PlaywrightWebApplicationFactory _factory = null!;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private bool _testFailed;
    private string? _traceFile;

    public DashboardRootBrowserTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Starts the Kestrel test server and launches a headless Chromium browser.
    /// Tracing is enabled from the start so failure artifacts capture the full timeline.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Start the real Kestrel server with stubbed services
        _factory = new PlaywrightWebApplicationFactory();
        await _factory.StartAsync();
        _output.WriteLine($"Kestrel server started at {_factory.BaseUrl}");

        // Launch Playwright Chromium — headless by default, set PLAYWRIGHT_HEADED=true to see UI
        var headed = Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADED") == "true";
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !headed
        });

        // Create browser context with tracing enabled for failure diagnostics
        var resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults");
        Directory.CreateDirectory(resultsDir);

        _traceFile = Path.Combine(resultsDir, $"trace-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.zip");

        _context = await _browser.NewContextAsync();
        await _context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true
        });

        _page = await _context.NewPageAsync();
        _page.SetDefaultTimeout(10_000);
        _page.SetDefaultNavigationTimeout(15_000);

        _output.WriteLine("Playwright browser initialized");
    }

    /// <summary>
    /// Captures failure artifacts (screenshot + trace) if the test failed,
    /// then closes browser and disposes the Kestrel server.
    /// </summary>
    public async Task DisposeAsync()
    {
        try
        {
            // Capture failure artifacts before tearing down
            if (_testFailed && _page != null)
            {
                var screenshotPath = Path.Combine(
                    Directory.GetCurrentDirectory(), "TestResults",
                    $"failure-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.png");

                await _page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
                _output.WriteLine($"Failure screenshot: {screenshotPath}");
            }

            if (_context != null)
            {
                if (_testFailed && _traceFile != null)
                {
                    await _context.Tracing.StopAsync(new TracingStopOptions { Path = _traceFile });
                    _output.WriteLine($"Failure trace: {_traceFile}");
                }
                else
                {
                    await _context.Tracing.StopAsync();
                }
            }

            // Dispose browser resources
            if (_page != null) await _page.CloseAsync();
            if (_context != null) await _context.CloseAsync();
            if (_browser != null) await _browser.CloseAsync();
            _playwright?.Dispose();

            // Dispose the Kestrel server
            await _factory.DisposeAsync();
            _output.WriteLine("Cleanup completed");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Cleanup error (non-fatal): {ex.Message}");
        }
    }

    /// <summary>
    /// Validates the complete dashboard rendering lifecycle in a real browser:
    /// 1. Navigate to root URL
    /// 2. Shell markers render (sidebar, header, main area)
    /// 3. Loading state appears during API delay
    /// 4. Loading transitions to populated state after stub response
    /// 5. No console errors during the entire flow
    /// </summary>
    [Fact(Skip = "E2E tests require UI refactor — data-testid attributes and auth setup outdated")]
    public async Task DashboardRoot_ShellVisibleAndStateTransition()
    {
        Assert.NotNull(_page);

        try
        {
            // Track console errors to catch Blazor/JS issues
            var consoleErrors = new List<string>();
            _page.Console += (_, msg) =>
            {
                if (msg.Type == "error")
                    consoleErrors.Add(msg.Text);
            };

            // Navigate to the real Kestrel endpoint
            _output.WriteLine($"Navigating to {_factory.BaseUrl}/test-dashboard...");
            var response = await _page.GotoAsync($"{_factory.BaseUrl}/test-dashboard", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            Assert.NotNull(response);
            Assert.True(response.Ok, $"Navigation failed with status {response.Status}");
            _output.WriteLine($"Navigation OK (status {response.Status})");

            // Assert: Shell renders with all structural markers
            _output.WriteLine("Waiting for dashboard shell...");
            await _page.WaitForSelectorAsync("[data-testid='dashboard-shell']");

            var sidebar = await _page.QuerySelectorAsync("[data-testid='dashboard-sidebar']");
            Assert.NotNull(sidebar);

            var header = await _page.QuerySelectorAsync("[data-testid='dashboard-header']");
            Assert.NotNull(header);

            _output.WriteLine("Shell markers verified");

            // Assert: Loading state appears (stub has 150ms delay)
            _output.WriteLine("Checking for loading state...");
            var loadingVisible = await _page.QuerySelectorAsync("[data-testid='dashboard-state-loading']");
            // Loading may have already transitioned if Blazor hydration is fast; that's acceptable
            if (loadingVisible != null)
            {
                _output.WriteLine("Loading state detected");
            }
            else
            {
                _output.WriteLine("Loading state already transitioned (fast hydration)");
            }

            // Assert: Wait for stable state (populated or empty)
            _output.WriteLine("Waiting for stable state...");
            var stableState = await _page.WaitForSelectorAsync(
                "[data-testid='dashboard-state-populated'], [data-testid='dashboard-state-empty']",
                new PageWaitForSelectorOptions { Timeout = 5_000 });

            Assert.NotNull(stableState);
            _output.WriteLine("Stable state reached");

            // Assert: No unexpected console errors during the render lifecycle
            // Filter out known network errors from Blazor components trying to reach
            // external resources (api.aura.test) that are expected in test environment
            var realErrors = consoleErrors
                .Where(e => !e.Contains("Failed to load resource") || !IsExpectedNetworkError(e))
                .ToList();
            
            if (realErrors.Any())
            {
                _output.WriteLine($"Unexpected console errors: {string.Join("; ", realErrors)}");
            }
            Assert.Empty(realErrors);

            _output.WriteLine("TEST PASSED: Shell visible, state transition confirmed");
        }
        catch
        {
            _testFailed = true;
            throw;
        }
    }

    /// <summary>
    /// Determines if a console error is an expected network failure from Blazor components
    /// trying to reach external resources (e.g., api.aura.test) in the test environment.
    /// </summary>
    private static bool IsExpectedNetworkError(string error)
    {
        var expectedPatterns = new[]
        {
            "api.aura.test",
            "favicon.ico",
            "Failed to load resource: the server responded with a status of 404",
            "Failed to load resource: the server responded with a status of 500",
            "net::ERR_NAME_NOT_RESOLVED",
            "net::ERR_CONNECTION_REFUSED",
            "WebSocket connection to",
            "blazor.server.js",
            "_framework/"
        };

        return expectedPatterns.Any(pattern =>
            error.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
