using Aura.Infrastructure.Adapters.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aura.IntegrationTests.Identity;

/// <summary>
/// Integration tests for <see cref="DependencyInjection.AddIdentityAdapter"/>.
/// Verifies both UseEntraId=true (OIDC metadata) and UseEntraId=false (symmetric key) branches.
/// </summary>
public class DependencyInjectionTests
{
    [Fact]
    public void AddIdentityAdapter_UseEntraIdFalse_ConfiguresSymmetricKey()
    {
        // Arrange
        var configValues = new Dictionary<string, string?>
        {
            ["UseEntraId"] = "false",
            ["MockJwt:Key"] = "test-key-at-least-32-characters-long!!",
            ["MockJwt:Issuer"] = "test-issuer",
            ["MockJwt:Audience"] = "test-audience"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddIdentityAdapter(configuration, new TestHostEnvironment(isDevelopment: true));
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                });
            })
            .Build();

        host.Start();

        // Act
        var options = host.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get("MockJwt");

        // Assert
        Assert.NotNull(options);
        Assert.NotNull(options.TokenValidationParameters);
        Assert.NotNull(options.TokenValidationParameters.IssuerSigningKey);
        Assert.IsType<SymmetricSecurityKey>(options.TokenValidationParameters.IssuerSigningKey);
        Assert.Equal("test-issuer", options.TokenValidationParameters.ValidIssuer);
        Assert.Equal("test-audience", options.TokenValidationParameters.ValidAudience);

        host.Dispose();
    }

    [Fact]
    public void AddIdentityAdapter_UseEntraIdTrue_ConfiguresMetadataAddress()
    {
        // Arrange
        const string tenantId = "test-tenant-123";
        var configValues = new Dictionary<string, string?>
        {
            ["UseEntraId"] = "true",
            ["AzureAd:TenantId"] = tenantId,
            ["MockJwt:Key"] = "test-key-at-least-32-characters-long!!"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddIdentityAdapter(configuration, new TestHostEnvironment(isDevelopment: true));
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                });
            })
            .Build();

        host.Start();

        // Act
        var options = host.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get("EntraId");

        // Assert
        Assert.NotNull(options);
        Assert.Contains(tenantId, options.MetadataAddress);
        Assert.Contains(".well-known/openid-configuration", options.MetadataAddress);

        host.Dispose();
    }

    [Fact]
    public void AddIdentityAdapter_AlwaysRegistersCurrentUserService()
    {
        // Arrange
        var configValues = new Dictionary<string, string?>
        {
            ["UseEntraId"] = "false",
            ["MockJwt:Key"] = "test-key-at-least-32-characters-long!!"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddIdentityAdapter(configuration, new TestHostEnvironment(isDevelopment: true));
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                });
            })
            .Build();

        host.Start();

        // Act
        var currentUserService = host.Services.GetService<Application.Ports.ICurrentUserService>();

        // Assert
        Assert.NotNull(currentUserService);
        Assert.IsType<HttpContextCurrentUserService>(currentUserService);

        host.Dispose();
    }

    [Fact]
    public void AddIdentityAdapter_UseEntraIdFalse_DoesNotConfigureOidcMetadata()
    {
        // Arrange — when UseEntraId is false, OIDC metadata endpoint must NOT be configured
        var configValues = new Dictionary<string, string?>
        {
            ["UseEntraId"] = "false",
            ["MockJwt:Key"] = "test-key-at-least-32-characters-long!!",
            ["MockJwt:Issuer"] = "test-issuer",
            ["MockJwt:Audience"] = "test-audience"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddIdentityAdapter(configuration, new TestHostEnvironment(isDevelopment: true));
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                });
            })
            .Build();

        host.Start();

        // Act
        var options = host.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get("EntraId");

        // Assert — MetadataAddress should be null/empty when UseEntraId=false
        Assert.NotNull(options);
        Assert.True(string.IsNullOrEmpty(options.MetadataAddress)
                     || options.MetadataAddress == "https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration");

        host.Dispose();
    }

    [Fact]
    public void AddIdentityAdapter_UseEntraIdTrue_DoesNotConfigureSymmetricKey()
    {
        // Arrange — when UseEntraId is true, symmetric signing key should NOT be set
        const string tenantId = "test-tenant-456";
        var configValues = new Dictionary<string, string?>
        {
            ["UseEntraId"] = "true",
            ["AzureAd:TenantId"] = tenantId,
            ["MockJwt:Key"] = "test-key-at-least-32-characters-long!!"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddIdentityAdapter(configuration, new TestHostEnvironment(isDevelopment: true));
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                });
            })
            .Build();

        host.Start();

        // Act
        var options = host.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get("EntraId");

        // Assert — IssuerSigningKey should be null when using OIDC metadata
        Assert.NotNull(options);
        Assert.Null(options.TokenValidationParameters.IssuerSigningKey);
        Assert.Contains(tenantId, options.MetadataAddress);

        host.Dispose();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(bool isDevelopment)
        {
            EnvironmentName = isDevelopment ? Environments.Development : Environments.Production;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
