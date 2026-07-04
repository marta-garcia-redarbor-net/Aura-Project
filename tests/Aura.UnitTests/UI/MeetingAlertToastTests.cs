using System.Reflection;
using Aura.UI.Components.Dashboard;
using Aura.UI.Services;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;

namespace Aura.UnitTests.UI;

public class MeetingAlertToastTests : TestContext
{
    [Fact]
    public void MeetingAlertToast_ShouldRender_Initially()
    {
        // Arrange
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton(Substitute.For<ITokenAcquisitionService>());
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        
        // Act
        var cut = RenderComponent<MeetingAlertToast>();

        // Assert
        Assert.NotNull(cut.Find("[data-testid='meeting-alert-toast']"));
    }

    [Fact]
    public void MeetingAlertToast_ShouldShowEmpty_WhenNoAlerts()
    {
        // Arrange
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton(Substitute.For<ITokenAcquisitionService>());
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        
        // Act
        var cut = RenderComponent<MeetingAlertToast>();

        // Assert - content element should not exist when no alerts
        var elements = cut.FindAll("[data-testid='meeting-alert-toast-content']");
        Assert.Empty(elements);
    }

    [Fact]
    public void MeetingAlertToast_ShouldDismissAlert_WhenAcknowledgeClicked()
    {
        // Arrange
        Services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        Services.AddSingleton(Substitute.For<ITokenAcquisitionService>());
        Services.AddSingleton(Substitute.For<IJSRuntime>());
        
        var cut = RenderComponent<MeetingAlertToast>();
        
        // Simulate an alert arriving by setting the private field via reflection.
        // HubConnection cannot be mocked in bUnit; this tests the acknowledge UI behavior
        // which triggers HubConnection.InvokeAsync("AcknowledgeAlert", alertId) when connected.
        var alertField = typeof(MeetingAlertToast).GetField("_currentAlert", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        alertField!.SetValue(cut.Instance, new MeetingAlertToast.MeetingAlertInfo
        {
            Id = "alert-1",
            Title = "Team Standup",
            Time = "10:00 AM"
        });
        
        cut.Render();
        
        // Verify alert is displayed
        Assert.NotNull(cut.Find("[data-testid='meeting-alert-title']"));
        Assert.Equal("Team Standup", cut.Find("[data-testid='meeting-alert-title']").TextContent);
        
        // Act — click acknowledge (sends AcknowledgeAlert to server via SignalR, then clears)
        cut.Find("[data-testid='meeting-alert-acknowledge']").Click();
        
        // Assert — alert is dismissed
        var elements = cut.FindAll("[data-testid='meeting-alert-toast-content']");
        Assert.Empty(elements);
    }
}
