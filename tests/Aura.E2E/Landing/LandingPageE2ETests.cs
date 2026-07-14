using Aura.E2E.Browser;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace Aura.E2E.Landing;

/// <summary>
/// E2E tests for the public landing page using Playwright.
/// Validates that anonymous users see the landing page at / and
/// that the page renders all expected sections.
/// </summary>
public class LandingPageE2ETests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private PlaywrightWebApplicationFactory _factory = null!;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private bool _testFailed;

    public LandingPageE2ETests(ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _factory = new PlaywrightWebApplicationFactory();
        await _factory.StartAsync();
        _output.WriteLine($"Kestrel server started at {_factory.BaseUrl}");

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        _context = await _browser.NewContextAsync();
        _page = await _context.NewPageAsync();
        _page.SetDefaultTimeout(10_000);
        _page.SetDefaultNavigationTimeout(15_000);
    }

    public async Task DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact(Skip = "Requires real browser infrastructure — Kestrel + SignalR handshake times out in test environment. HTTP-only smoke tests provide equivalent coverage.")]
    public async Task AnonymousUser_SeesLandingPageAtRoot()
    {
        Assert.NotNull(_page);

        try
        {
            var response = await _page.GotoAsync($"{_factory.BaseUrl}/", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            Assert.NotNull(response);
            Assert.True(response.Ok, $"Navigation failed with status {response.Status}");

            // Assert — landing page sections are visible
            await _page.WaitForSelectorAsync("[data-testid='landing-hero']");
            var hero = await _page.QuerySelectorAsync("[data-testid='landing-hero']");
            Assert.NotNull(hero);

            // Assert — hero has both CTAs
            var loginBtn = await _page.QuerySelectorAsync("[data-testid='hero-login-btn']");
            Assert.NotNull(loginBtn);

            var demoBtn = await _page.QuerySelectorAsync("[data-testid='hero-demo-btn']");
            Assert.NotNull(demoBtn);

            _output.WriteLine("TEST PASSED: Landing page renders for anonymous user");
        }
        catch
        {
            _testFailed = true;
            throw;
        }
    }

    [Fact(Skip = "Requires real browser infrastructure — Kestrel + SignalR handshake times out in test environment. HTTP-only smoke tests provide equivalent coverage.")]
    public async Task LandingPage_HasAllSections()
    {
        Assert.NotNull(_page);

        try
        {
            await _page.GotoAsync($"{_factory.BaseUrl}/", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // Assert — all major sections exist
            await _page.WaitForSelectorAsync("[data-testid='landing-header']");
            await _page.WaitForSelectorAsync("[data-testid='landing-hero']");
            await _page.WaitForSelectorAsync("[data-testid='landing-problems']");
            await _page.WaitForSelectorAsync("[data-testid='landing-features']");
            await _page.WaitForSelectorAsync("[data-testid='landing-cta']");
            await _page.WaitForSelectorAsync("[data-testid='landing-footer']");

            _output.WriteLine("TEST PASSED: All landing page sections render");
        }
        catch
        {
            _testFailed = true;
            throw;
        }
    }
}
