using System.Net;
using System.Security.Claims;
using Aura.UI.Components.Dashboard;
using Aura.UI.Components.Layout;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

/// <summary>
/// Tests for the claim-based demo mode visibility in PriorityDashboard.
/// Demo controls are shown/hidden based on the aura_demo_mode claim, not config.
/// </summary>
public class PriorityDashboardDemoClaimTests : TestContext
{
    private sealed class AlwaysAuthorizedService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Success());

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(AuthorizationResult.Success());
    }

    private static AuthenticationState CreateAuthState(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    private void SetupServices()
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, AlwaysAuthorizedService>();
        Services.AddSingleton<IDashboardEventBus>(new DashboardEventBus());
        Services.AddSingleton<IDashboardRealtimeStatus>(new DashboardRealtimeStatus());
        Services.AddSingleton(new DemoUiState());

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient(new StubHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost:5180/")
        };
        httpClientFactory.CreateClient("AuraApi").Returns(httpClient);
        Services.AddSingleton(httpClientFactory);

        var summary = Substitute.For<IPrioritySummaryService>();
        summary.GetCardsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<PrioritySummaryCard>()));
        Services.AddSingleton(summary);

        var previewClient = Substitute.For<IDashboardPreviewApiClient>();
        previewClient.GetPreviewAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DashboardPreviewResponse([], [])));
        Services.AddSingleton(previewClient);

        var statusClient = Substitute.For<ISystemStatusApiClient>();
        statusClient.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SystemStatusResponse(
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "API ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "DB ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "Qdrant ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "LLM ok"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "MockAuth ok"))));
        Services.AddSingleton(statusClient);

        var focusStateApi = Substitute.For<IFocusStateApiClient>();
        focusStateApi.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new FocusStateResponse("WindowOfOpportunity", false, "user-123")));
        Services.AddSingleton(focusStateApi);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
    }

    [Fact]
    public void DemoClaim_Present_ShowsDemoControls()
    {
        // Arrange — user has aura_demo_mode=true claim
        SetupServices();
        var demoState = new DemoUiState { IsDemoUser = true };
        Services.AddSingleton(demoState);
        
        var authState = CreateAuthState(
            new Claim(ClaimTypes.Name, "Demo User"),
            new Claim("aura_demo_mode", "true"));
        Task<AuthenticationState> authStateTask = Task.FromResult(authState);

        // Act — render Header component which shows demo controls
        var cut = RenderComponent<Aura.UI.Components.Layout.Header>(p => p.AddCascadingValue(authStateTask));

        // Assert — demo buttons are visible
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("button.dashboard-header__demo-btn"));
        }, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void DemoClaim_Absent_HidesDemoControls()
    {
        // Arrange — user has NO aura_demo_mode claim (real Entra ID auth)
        SetupServices();
        var demoState = new DemoUiState { IsDemoUser = false };
        Services.AddSingleton(demoState);
        
        var authState = CreateAuthState(
            new Claim(ClaimTypes.Name, "Real User"));
        Task<AuthenticationState> authStateTask = Task.FromResult(authState);

        // Act — render Header component
        var cut = RenderComponent<Aura.UI.Components.Layout.Header>(p => p.AddCascadingValue(authStateTask));

        // Assert — demo buttons are NOT rendered
        cut.WaitForAssertion(() =>
        {
            var demoButtons = cut.FindAll("button.dashboard-header__demo-btn");
            Assert.Empty(demoButtons);
        }, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void DemoClaim_False_HidesDemoControls()
    {
        // Arrange — user has aura_demo_mode=false (not "true")
        SetupServices();
        var demoState = new DemoUiState { IsDemoUser = false };
        Services.AddSingleton(demoState);
        
        var authState = CreateAuthState(
            new Claim(ClaimTypes.Name, "Some User"),
            new Claim("aura_demo_mode", "false"));
        Task<AuthenticationState> authStateTask = Task.FromResult(authState);

        // Act — render Header component
        var cut = RenderComponent<Aura.UI.Components.Layout.Header>(p => p.AddCascadingValue(authStateTask));

        // Assert — demo buttons are NOT rendered
        cut.WaitForAssertion(() =>
        {
            var demoButtons = cut.FindAll("button.dashboard-header__demo-btn");
            Assert.Empty(demoButtons);
        }, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void DemoClaim_Present_ShowsResetButton()
    {
        // Arrange — user has aura_demo_mode=true claim
        SetupServices();
        var demoState = new DemoUiState { IsDemoUser = true };
        Services.AddSingleton(demoState);
        
        var authState = CreateAuthState(
            new Claim(ClaimTypes.Name, "Demo User"),
            new Claim("aura_demo_mode", "true"));
        Task<AuthenticationState> authStateTask = Task.FromResult(authState);

        // Act — render Header component
        var cut = RenderComponent<Aura.UI.Components.Layout.Header>(p => p.AddCascadingValue(authStateTask));

        // Assert — reset button is visible
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("button.dashboard-header__reset-btn"));
        }, TimeSpan.FromSeconds(2));
    }
}
