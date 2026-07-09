using System.Reflection;
using Aura.UI.Components.Dashboard;
using Aura.UI.Components.Pages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace Aura.ArchitectureTests;

/// <summary>
/// Architecture tests for the landing page rework.
/// Validates that LandingPage has [AllowAnonymous] and PriorityDashboard has the correct route.
/// </summary>
public class LandingPageArchitectureTests
{
    [Fact]
    public void LandingPage_HasAllowAnonymousAttribute()
    {
        // Arrange & Act
        var attribute = typeof(LandingPage).GetCustomAttribute<AllowAnonymousAttribute>();

        // Assert — LandingPage must be publicly accessible
        Assert.NotNull(attribute);
    }

    [Fact]
    public void PriorityDashboard_HasDashboardRoute()
    {
        // Arrange & Act
        var routeAttributes = typeof(PriorityDashboard)
            .GetCustomAttributes<RouteAttribute>()
            .ToList();

        // Assert — PriorityDashboard must have /dashboard route
        Assert.Contains(routeAttributes, r => r.Template == "/dashboard");
    }

    [Fact]
    public void PriorityDashboard_DoesNotHaveRootRoute()
    {
        // Arrange & Act
        var routeAttributes = typeof(PriorityDashboard)
            .GetCustomAttributes<RouteAttribute>()
            .ToList();

        // Assert — PriorityDashboard must NOT have / route (that's the landing page now)
        Assert.DoesNotContain(routeAttributes, r => r.Template == "/");
    }

    [Fact]
    public void LandingPage_HasRootRoute()
    {
        // Arrange & Act
        var routeAttributes = typeof(LandingPage)
            .GetCustomAttributes<RouteAttribute>()
            .ToList();

        // Assert — LandingPage must serve at /
        Assert.Contains(routeAttributes, r => r.Template == "/");
    }
}
