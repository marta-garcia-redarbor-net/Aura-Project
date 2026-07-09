using Aura.Infrastructure;
using Aura.Infrastructure.Adapters.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Aura.UnitTests.Identity;

/// <summary>
/// Unit tests for dual JWT Bearer scheme registration.
/// Verifies that both "EntraId" and "MockJwt" schemes are registered simultaneously.
/// </summary>
public class DualJwtSchemeRegistrationTests
{
    private static IConfiguration CreateConfig(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Qdrant:Host"] = "test-host",
            ["Qdrant:GrpcPort"] = "6334",
            ["Qdrant:VectorSize"] = "768",
            ["ConnectionStrings:SemanticOutbox"] = "Data Source=:memory:",
            ["ConnectionStrings:Aura"] = "Data Source=:memory:",
            ["EmbeddingProvider:Endpoint"] = "https://test.openai.azure.com",
            ["EmbeddingProvider:DeploymentName"] = "text-embedding-ada-002",
            ["EmbeddingProvider:ApiKey"] = "test-key",
            ["UseEntraId"] = "false",
            ["MockJwt:Key"] = "aura-test-key-for-unit-tests-minimum-32-chars!!",
            ["MockJwt:Issuer"] = "aura-dev",
            ["MockJwt:Audience"] = "aura-api",
            ["AzureAd:ClientId"] = "test-client-id",
            ["AzureAd:TenantId"] = "test-tenant-id"
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
                values[key] = value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values!)
            .Build();
    }

    private static IHostEnvironment CreateDevEnvironment()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Development);
        return env;
    }

    [Fact]
    public void AddIdentityAdapter_RegistersBothEntraIdAndMockJwtSchemes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(CreateConfig());
        services.AddSingleton<IHostEnvironment>(CreateDevEnvironment());
        services.AddLogging();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());

        var provider = services.BuildServiceProvider();

        // Act
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var entraScheme = schemeProvider.GetSchemeAsync("EntraId").GetAwaiter().GetResult();
        var mockScheme = schemeProvider.GetSchemeAsync("MockJwt").GetAwaiter().GetResult();

        // Assert — both schemes MUST be registered simultaneously
        Assert.NotNull(entraScheme);
        Assert.NotNull(mockScheme);
    }

    [Fact]
    public void AddIdentityAdapter_EntraIdIsDefaultScheme()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());
        var provider = services.BuildServiceProvider();

        // Act
        var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
        var defaultScheme = schemeProvider.GetDefaultAuthenticateSchemeAsync()
            .GetAwaiter().GetResult();

        // Assert — EntraId MUST be the default scheme
        Assert.NotNull(defaultScheme);
        Assert.Equal("EntraId", defaultScheme.Name);
    }

    [Fact]
    public void AddIdentityAdapter_DefaultPolicyAcceptsBothSchemes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());
        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>();
        var defaultPolicy = options.Value.DefaultPolicy;

        // Assert — default policy must list both schemes
        Assert.NotNull(defaultPolicy);
        Assert.Contains("EntraId", defaultPolicy.AuthenticationSchemes);
        Assert.Contains("MockJwt", defaultPolicy.AuthenticationSchemes);
    }

    [Fact]
    public async Task AddIdentityAdapter_DemoOnlyPolicyRequiresMockJwtScheme()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());
        var provider = services.BuildServiceProvider();

        // Act
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var demoPolicy = await policyProvider.GetPolicyAsync("DemoOnly");

        // Assert — DemoOnly policy must require MockJwt scheme
        Assert.NotNull(demoPolicy);
        Assert.Contains("MockJwt", demoPolicy.AuthenticationSchemes);
    }

    [Fact]
    public void AddIdentityAdapter_MockJwtGeneratorRegisteredRegardlessOfEnvironment()
    {
        // Arrange — production environment
        var prodEnv = Substitute.For<IHostEnvironment>();
        prodEnv.EnvironmentName.Returns(Environments.Production);

        var services = new ServiceCollection();
        services.AddAuraInfrastructure(CreateConfig(), prodEnv);
        var provider = services.BuildServiceProvider();

        // Act
        var generator = provider.GetService<MockJwtGenerator>();

        // Assert — generator MUST be registered in all environments
        Assert.NotNull(generator);
    }
}
