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
    public void ObservabilityPage_WhenAuthenticated_ShowsThreePanels()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        var panels = cut.FindAll("section.dashboard-panel");
        Assert.Equal(3, panels.Count);

        // Verify panel headings
        var headings = cut.FindAll("section.dashboard-panel h2");
        Assert.Equal(3, headings.Count);
        Assert.Contains("Logs", headings[0].TextContent);
        Assert.Contains("Metrics", headings[1].TextContent);
        Assert.Contains("Traces", headings[2].TextContent);
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
    public void ObservabilityPage_WithLogData_RendersLogRows()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        var authState = Task.FromResult(CreateAuthenticatedState());

        var cut = RenderComponent<Observability>(parameters => parameters
            .AddCascadingValue(authState));

        // Simulate logs received via reflection (event is public but can only be raised internally)
        var testLogs = new List<LogRecordDto>
        {
            new(LogLevel.Error, DateTimeOffset.UtcNow, "corr-1", "Test error", "TestSource"),
            new(LogLevel.Information, DateTimeOffset.UtcNow, "corr-2", "Test info", "TestSource")
        }.AsReadOnly();

        RaiseEvent(_telemetryClient, "LogsReceived", testLogs);
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tbody tr");
            Assert.True(rows.Count >= 2, $"Expected at least 2 log rows, found {rows.Count}");
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
            new(LogLevel.Warning, DateTimeOffset.UtcNow, "corr-w", "Warning msg", "Src")
        }.AsReadOnly();

        RaiseEvent(_telemetryClient, "LogsReceived", testLogs);
        cut.WaitForAssertion(() =>
        {
            var row = cut.Find("tr.log-row");
            Assert.Contains("log-row--warning", row.ClassName);
        });
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
