using System.Net;
using Aura.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Aura.IntegrationTests.Auth;

/// <summary>
/// Integration tests for the /login/demo minimal API endpoint.
/// Validates that the endpoint creates a demo auth cookie and redirects to /dashboard.
/// </summary>
public class DemoLoginEndpointTests : IClassFixture<WebApplicationFactory<UiMarker>>
{
    private readonly WebApplicationFactory<UiMarker> _factory;

    public DemoLoginEndpointTests(WebApplicationFactory<UiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("UseEntraId", "false");
        });
    }

    [Fact]
    public async Task GetLoginDemo_Returns302RedirectToDashboard()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/login/demo");

        // Assert — must redirect to /dashboard
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/dashboard", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GetLoginDemo_SetsCookieWithDemoClaim()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/login/demo");

        // Assert — Set-Cookie header must be present
        Assert.True(response.Headers.Contains("Set-Cookie"),
            "Expected Set-Cookie header to set the authentication cookie");
        var cookies = response.Headers.GetValues("Set-Cookie");
        Assert.Contains(cookies, c =>
            c.Contains(".AspNetCore.Cookies") || c.Contains("Cookies"));
    }

    [Fact]
    public async Task GetLoginDemo_AccessibleWithoutAuth()
    {
        // Arrange — no authentication cookie set
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/login/demo");

        // Assert — endpoint is accessible (not 401/403)
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
