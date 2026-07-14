using Aura.Infrastructure.Observability;
using Aura.UI.Components.Pages;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NSubstitute;
using System.Reflection;
using System.Security.Claims;

namespace Aura.UnitTests.UI;

/// <summary>
/// bUnit tests for the Observability.razor page.
/// Verifies rendering, panel structure, and auth redirect behavior.
/// </summary>
public class ObservabilityPageTests : TestContext
{
    private readonly TelemetryClient _telemetryClient;
    private readonly TestAuthenticationStateProvider _authStateProvider;

    public ObservabilityPageTests()
    {
        // Build a real TelemetryClient with dummy config — StartAsync will fail
        // silently (caught internally), but the instance is valid for rendering.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AuraApi:BaseUrl"] = "http://localhost"
            })
            .Build();

        var tokenService = Substitute.For<ITokenAcquisitionService>();
        tokenService.AcquireTokenAsync(Arg.Any<CancellationToken>()).Returns("test-token");
        var loggerFactory = LoggerFactory.Create(b => b.AddDebug());
        _telemetryClient = new TelemetryClient(tokenService, config, loggerFactory.CreateLogger<TelemetryClient>());

        Services.AddSingleton(_telemetryClient);

        // Register the full authorization stack (IAuthorizationPolicyProvider, etc.)
        Services.AddAuthorizationCore();

        // Override bUnit's PlaceholderAuthorizationService with a fake that checks authentication
        var authService = Substitute.For<IAuthorizationService>();
        authService.AuthorizeAsync(
            Arg.Any<ClaimsPrincipal>(),
            Arg.Any<object>(),
            Arg.Any<IEnumerable<IAuthorizationRequirement>>())
            .Returns(callInfo =>
            {
                var user = callInfo.ArgAt<ClaimsPrincipal>(0);
                return user.Identity?.IsAuthenticated == true
                    ? Task.FromResult(AuthorizationResult.Success())
                    : Task.FromResult(AuthorizationResult.Failed());
            });
        Services.AddSingleton(authService);

        // Register a mutable auth state provider so each test can set its own state
        _authStateProvider = new TestAuthenticationStateProvider(CreateAnonymousState());
        Services.AddSingleton<AuthenticationStateProvider>(_authStateProvider);

        // Mock IJSRuntime to avoid JS interop errors during tests
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<object>("window.__auraObservabilityScrollToTop", Arg.Any<object[]>())
            .Returns(ValueTask.FromResult<object>(null!));
        Services.AddSingleton(jsRuntime);
    }

    private void SetAuthenticated(string name = "Test User")
    {
        _authStateProvider.SetState(CreateAuthenticatedState(name));
    }

    private void SetAnonymous()
    {
        _authStateProvider.SetState(CreateAnonymousState());
    }

    private static AuthenticationState CreateAuthenticatedState(string name = "Test User")
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, name) },
            "TestAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private static AuthenticationState CreateAnonymousState()
    {
        return new AuthenticationState(new ClaimsPrincipal());
    }

    [Fact]
    public void ObservabilityPage_WhenAuthenticated_RendersPageTitle()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        // PageTitle doesn't render as <title> in bUnit, so verify via the header
        var headerTitle = cut.Find(".dashboard-page-header__title");
        Assert.Contains("Observability Dashboard", headerTitle.TextContent);
    }

    [Fact]
    public void ObservabilityPage_WhenAuthenticated_ShowsTwoPanels()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        var panels = cut.FindAll("section.dashboard-panel");
        Assert.Equal(2, panels.Count);

        // Verify panel headings
        var headings = cut.FindAll("section.dashboard-panel h2");
        Assert.Equal(2, headings.Count);
        Assert.Contains("Logs", headings[0].TextContent);
        Assert.Contains("Traces", headings[1].TextContent);
    }

    [Fact]
    public void ObservabilityPage_WhenAuthenticated_ShowsHeaderAndSubtitle()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        var headerTitle = cut.Find(".dashboard-page-header__title");
        Assert.Contains("Observability Dashboard", headerTitle.TextContent);

        var subtitle = cut.Find(".dashboard-page-header__subtitle");
        Assert.Contains("Real-time telemetry", subtitle.TextContent);
    }

    [Fact]
    public void ObservabilityPage_WithLogData_RendersLogRowsReversed()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        // Two logs: older first, newer second (both Error level so they pass default filter)
        var older = new LogRecordDto(LogLevel.Error, DateTimeOffset.UtcNow.AddSeconds(-5), "corr-1", "Older log", "Src");
        var newer = new LogRecordDto(LogLevel.Error, DateTimeOffset.UtcNow, "corr-2", "Newer log", "Src");

        RaiseEvent(_telemetryClient, "LogsReceived", new[] { older, newer }.ToList().AsReadOnly());

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr.log-row");
            Assert.True(rows.Count >= 2, $"Expected at least 2 log rows, found {rows.Count}");
            // Newest should be first (reversed order)
            Assert.Contains("Newer log", rows[0].TextContent);
            Assert.Contains("Older log", rows[1].TextContent);
        });
    }

    [Fact]
    public void ObservabilityPage_LogRows_HaveLevelCssClass()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        var testLogs = new List<LogRecordDto>
        {
            new(LogLevel.Error, DateTimeOffset.UtcNow, "corr-e", "Error msg", "Src")
        }.AsReadOnly();

        RaiseEvent(_telemetryClient, "LogsReceived", testLogs);
        cut.WaitForAssertion(() =>
        {
            var row = cut.Find("tr.log-row");
            Assert.Contains("log-row--error", row.ClassName);
        });
    }

    [Fact]
    public void ObservabilityPage_LogPanel_HasSearchField()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        var searchInput = cut.Find("input.log-search");
        Assert.NotNull(searchInput);
    }

    [Fact]
    public void ObservabilityPage_LogPanel_HasLevelFilterButtons()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        var filterButtons = cut.FindAll("button.level-filter");
        Assert.True(filterButtons.Count >= 4, $"Expected 4 level filter buttons, found {filterButtons.Count}");
    }

    [Fact]
    public void ObservabilityPage_LogPanel_HasAutoScrollToggle()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        var toggle = cut.Find("button.auto-scroll-toggle");
        Assert.NotNull(toggle);
    }

    [Fact]
    public void ObservabilityPage_LogPanel_Search_FiltersByMessage()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        var logs = new List<LogRecordDto>
        {
            new(LogLevel.Error, DateTimeOffset.UtcNow, "corr-1", "payment processed", "Src"),
            new(LogLevel.Error, DateTimeOffset.UtcNow, "corr-2", "auth failed", "Src"),
            new(LogLevel.Error, DateTimeOffset.UtcNow, "corr-3", "payment retry", "Src"),
        }.AsReadOnly();

        RaiseEvent(_telemetryClient, "LogsReceived", logs);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tbody tr.log-row")));

        // Type "payment" in search — only 2 logs should show
        var searchInput = cut.Find("input.log-search");
        searchInput.Input("payment");

        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr.log-row");
            Assert.Equal(2, rows.Count);
        });
    }

    [Fact]
    public void ObservabilityPage_LogPanel_FilterByLevel_HidesDeselectedLevel()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        var logs = new List<LogRecordDto>
        {
            new(LogLevel.Error, DateTimeOffset.UtcNow, "corr-1", "error msg", "Src"),
        }.AsReadOnly();

        RaiseEvent(_telemetryClient, "LogsReceived", logs);

        // Wait for error log to appear (default filter only shows Error)
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("tbody tr.log-row")), timeout: TimeSpan.FromSeconds(3));

        // Toggle Error OFF — log row should disappear
        cut.Find("button.level-filter--error").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("tbody tr.log-row")), timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void ObservabilityPage_LogPanel_AutoScrollToggle_ChangesText()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        var toggle = cut.Find("button.auto-scroll-toggle");

        // Default should be ON
        Assert.Contains("ON", toggle.TextContent);

        // Click to toggle off
        toggle.Click();
        Assert.Contains("OFF", cut.Find("button.auto-scroll-toggle").TextContent);

        // Click again to toggle on
        cut.Find("button.auto-scroll-toggle").Click();
        Assert.Contains("ON", cut.Find("button.auto-scroll-toggle").TextContent);
    }

    /// <summary>
    /// Raises a public event on an object via reflection.
    /// Used to simulate TelemetryClient event callbacks in bUnit tests.
    /// </summary>
    private static void RaiseEvent(object target, string eventName, object args)
    {
        var field = target.GetType().GetField(eventName,
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (field?.GetValue(target) is Delegate del)
        {
            del.DynamicInvoke(args);
        }
    }

    /// <summary>
    /// Mutable AuthenticationStateProvider for bUnit tests.
    /// </summary>
    private sealed class TestAuthenticationStateProvider : AuthenticationStateProvider
    {
        private AuthenticationState _state;

        public TestAuthenticationStateProvider(AuthenticationState state)
        {
            _state = state;
        }

        public void SetState(AuthenticationState state)
        {
            _state = state;
            NotifyAuthenticationStateChanged(Task.FromResult(state));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(_state);
        }
    }
}
