using System.Net;
using Aura.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Aura.IntegrationTests.Auth;

/// <summary>
/// Integration tests for the /login/challenge minimal API endpoint.
/// Validates that anonymous GET requests trigger an OIDC challenge (302 to Entra ID)
/// and that the correlation cookie is set by the OIDC middleware.
/// Uses a static OIDC configuration to avoid live network calls to Entra.
/// </summary>
public class LoginChallengeEndpointTests : IClassFixture<WebApplicationFactory<UiMarker>>
{
    private readonly WebApplicationFactory<UiMarker> _factory;

    public LoginChallengeEndpointTests(WebApplicationFactory<UiMarker> factory)
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
                // Replace the OIDC configuration manager with a static implementation that
                // returns a pre-loaded OpenIdConnectConfiguration, avoiding live HTTP calls
                // to login.microsoftonline.com during integration tests.
                services.PostConfigure<OpenIdConnectOptions>(
                    OpenIdConnectDefaults.AuthenticationScheme,
                    options =>
                    {
                        OpenIdConnectConfiguration staticConfig = new()
                        {
                            AuthorizationEndpoint =
                                "https://login.microsoftonline.com/test-tenant-id/oauth2/v2.0/authorize",
                            TokenEndpoint =
                                "https://login.microsoftonline.com/test-tenant-id/oauth2/v2.0/token",
                            Issuer = "https://login.microsoftonline.com/test-tenant-id/v2.0"
                        };

                        // Replace the configuration manager with a static one so the handler
                        // never attempts to fetch the OIDC discovery document.
                        options.ConfigurationManager =
                            new StaticConfigurationManager<OpenIdConnectConfiguration>(staticConfig);
                    });
            });
        });
    }

    [Fact]
    public async Task GetLoginChallenge_Anonymous_Returns302WithEntraLocation()
    {
        // Arrange — no redirect follow so we can inspect the 302 directly
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        HttpResponseMessage response = await client.GetAsync("/login/challenge");

        // Assert — OIDC challenge must redirect to Entra ID
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("login.microsoftonline.com", response.Headers.Location!.Host);
    }

    [Fact]
    public async Task GetLoginChallenge_Anonymous_SetsCorrelationCookie()
    {
        // Arrange
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        HttpResponseMessage response = await client.GetAsync("/login/challenge");

        // Assert — OIDC middleware must set correlation cookie for CSRF protection
        Assert.True(response.Headers.Contains("Set-Cookie"),
            "Expected Set-Cookie header with OIDC correlation cookie");
        IEnumerable<string> cookies = response.Headers.GetValues("Set-Cookie");
        Assert.Contains(cookies, c => c.Contains(".AspNetCore.Correlation."));
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A static IConfigurationManager that always returns a pre-set configuration,
    /// avoiding any network calls during tests.
    /// </summary>
    private sealed class StaticConfigurationManager<T>(T configuration)
        : IConfigurationManager<T>
        where T : class
    {
        public Task<T> GetConfigurationAsync(CancellationToken cancel) =>
            Task.FromResult(configuration);

        public void RequestRefresh() { }
    }
}
