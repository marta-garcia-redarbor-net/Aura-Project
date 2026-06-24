using Aura.UI.Components.Dashboard;
using Aura.UI.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;

namespace Aura.UnitTests.UI;

/// <summary>
/// Integration tests verifying SignalR hub connection wiring in MeetingAlertToast.
/// Full hub connectivity requires a test server with MeetingAlertHub; these tests
/// verify the integration points (token acquisition, connection configuration, error handling).
/// </summary>
public class SignalRMeetingAlertIntegrationTests : TestContext
{
    [Fact]
    public void SignalR_ShouldAcquireToken_DuringInitialization()
    {
        // Arrange
        var tokenService = Substitute.For<ITokenAcquisitionService>();
        tokenService.AcquireTokenAsync(Arg.Any<CancellationToken>())
            .Returns("integration-test-token");
        Services.AddSingleton(tokenService);
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        
        // Act
        var cut = RenderComponent<MeetingAlertToast>();
        
        // Assert — token service was called to provide auth token for hub connection
        tokenService.Received(1).AcquireTokenAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SignalR_ShouldHandleConnectionFailure_Gracefully()
    {
        // Arrange — token service returns a valid token, but hub URL doesn't exist in test
        var tokenService = Substitute.For<ITokenAcquisitionService>();
        tokenService.AcquireTokenAsync(Arg.Any<CancellationToken>())
            .Returns("test-token");
        Services.AddSingleton(tokenService);
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        
        // Act — should not throw even though /hubs/meeting-alerts doesn't exist
        var cut = RenderComponent<MeetingAlertToast>();
        
        // Assert — component rendered successfully despite connection failure
        Assert.NotNull(cut.Find("[data-testid='meeting-alert-toast']"));
        var contentElements = cut.FindAll("[data-testid='meeting-alert-toast-content']");
        Assert.Empty(contentElements);
    }
}
