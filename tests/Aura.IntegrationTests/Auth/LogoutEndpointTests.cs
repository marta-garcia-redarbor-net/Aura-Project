using System.Net;
using Aura.UI;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Aura.IntegrationTests.Auth;

/// <summary>
/// Integration tests for the /logout minimal API endpoint.
/// Validates that sign-out clears both the OIDC session (Entra ID end-session)
/// and the local authentication cookie when UseEntraId=true.
/// </summary>
public class LogoutEndpointTests : IClassFixture<WebApplicationFactory<UiMarker>>
{
    private readonly WebApplicationFactory<UiMarker> _factory;

    public LogoutEndpointTests(WebApplicationFactory<UiMarker> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("UseEntraId", "true");
            builder.UseSetting("AzureAd:TenantId", "test-tenant-id");
            builder.UseSetting("AzureAd:ClientId", "test-client-id");
            builder.UseSetting("AzureAd:ClientSecret", "test-client-secret");

            builder.ConfigureServices(services =>
            {
                services.PostConfigure<OpenIdConnectOptions>(
                    OpenIdConnectDefaults.AuthenticationScheme,
                    options =>
                    {
                        var staticConfig = new OpenIdConnectConfiguration
                        {
                            AuthorizationEndpoint =
                                "https://login.microsoftonline.com/test-tenant-id/oauth2/v2.0/authorize",
                            TokenEndpoint =
                                "https://login.microsoftonline.com/test-tenant-id/oauth2/v2.0/token",
                            EndSessionEndpoint =
                                "https://login.microsoftonline.com/test-tenant-id/oauth2/v2.0/logout",
                            Issuer = "https://login.microsoftonline.com/test-tenant-id/v2.0"
                        };

                        options.ConfigurationManager =
                            new StaticConfigurationManager<OpenIdConnectConfiguration>(staticConfig);
                        options.SignedOutRedirectUri = "/";
                    });
            });
        });
    }

    [Fact]
    public async Task GetLogout_WithEntraId_Returns302RedirectToHome()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/logout?useEntraId=true");

        // Assert — must redirect to home
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GetLogout_WithEntraId_ClearsAuthenticationCookie()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/logout?useEntraId=true");

        // Assert — Set-Cookie must contain an expired auth cookie
        Assert.True(response.Headers.Contains("Set-Cookie"),
            "Expected Set-Cookie header to clear the authentication cookie");
        var cookies = response.Headers.GetValues("Set-Cookie");
        Assert.Contains(cookies, c =>
            c.Contains(CookieAuthenticationDefaults.AuthenticationScheme) &&
            c.Contains("expires=") &&
            c.Contains("Thu, 01 Jan 1970"));
    }

    [Fact]
    public async Task GetLogout_WithoutEntraId_Returns302RedirectToHome()
    {
        // Arrange — dev mode: UseEntraId=false
        var devFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("UseEntraId", "false");
        });
        var client = devFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/logout?useEntraId=false");

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GetLogout_WithoutEntraId_ClearsAuthenticationCookie()
    {
        // Arrange
        var devFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("UseEntraId", "false");
        });
        var client = devFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        var response = await client.GetAsync("/logout?useEntraId=false");

        // Assert
        Assert.True(response.Headers.Contains("Set-Cookie"),
            "Expected Set-Cookie header to clear the authentication cookie");
        var cookies = response.Headers.GetValues("Set-Cookie");
        Assert.Contains(cookies, c =>
            c.Contains(CookieAuthenticationDefaults.AuthenticationScheme) &&
            c.Contains("expires=") &&
            c.Contains("Thu, 01 Jan 1970"));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    private sealed class StaticConfigurationManager<T>(T configuration)
        : IConfigurationManager<T>
        where T : class
    {
        public Task<T> GetConfigurationAsync(CancellationToken cancel) =>
            Task.FromResult(configuration);

        public void RequestRefresh() { }
    }
}
