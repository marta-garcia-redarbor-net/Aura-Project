using Aura.UI.Components.Dashboard;
using Aura.UI.Components.GraphConnector;
using Aura.UI.Models;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

/// <summary>
/// Tests that <see cref="Health"/> page renders the three health panels
/// when the user is authenticated.
/// </summary>
public class HealthPageTests : TestContext
{
    /// <summary>
    /// Minimal IAuthorizationService that always returns Authorized.
    /// </summary>
    private sealed class AlwaysAuthorizedService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            System.Security.Claims.ClaimsPrincipal user, object? resource,
            IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Success());

        public Task<AuthorizationResult> AuthorizeAsync(
            System.Security.Claims.ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(AuthorizationResult.Success());
    }

    private void SetupCommonServices()
    {
        // Authorization
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, AlwaysAuthorizedService>();

        // GraphConnectorStatusPanel dependency
        var graphApi = Substitute.For<IGraphConnectorApiClient>();
        graphApi.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GraphConnectorStatusResponse("ValidConfig")));
        Services.AddSingleton<IGraphConnectorApiClient>(graphApi);

        // SystemStatusPanel dependency
        var systemStatusApi = Substitute.For<ISystemStatusApiClient>();
        systemStatusApi.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SystemStatusResponse(
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "API running"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Ok, "Qdrant healthy"),
                new SystemIndicatorResponse(SystemIndicatorStateResponse.Warning, "Mock auth"))));
        Services.AddSingleton<ISystemStatusApiClient>(systemStatusApi);

        // ModuleProgressPanel dependency
        var moduleApi = Substitute.For<IModuleProgressApiClient>();
        moduleApi.GetAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ModuleProgressResponse(
                new List<ModuleEntryResponse>(), false)));
        Services.AddSingleton<IModuleProgressApiClient>(moduleApi);
    }

    private static AuthenticationState CreateAuthorizedState()
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Test User") },
            "TestAuth");
        return new AuthenticationState(new System.Security.Claims.ClaimsPrincipal(identity));
    }

    [Fact]
    public void Health_RendersThreePanels()
    {
        // Arrange
        SetupCommonServices();
        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());

        // Act
        var cut = RenderComponent<Aura.UI.Pages.Health>(parameters => parameters
            .AddCascadingValue(authStateTask));

        // Assert: all three health panels are present (by data-testid)
        Assert.Contains("graph-connector-panel", cut.Markup);
        Assert.Contains("system-status-panel", cut.Markup);
        Assert.Contains("module-progress-panel", cut.Markup);
    }

    [Fact]
    public void Health_HasCorrectPageTitle()
    {
        // Arrange
        SetupCommonServices();
        Task<AuthenticationState> authStateTask = Task.FromResult(CreateAuthorizedState());

        // Act
        var cut = RenderComponent<Aura.UI.Pages.Health>(parameters => parameters
            .AddCascadingValue(authStateTask));

        // Assert
        Assert.Contains("System Health", cut.Markup);
    }
}
