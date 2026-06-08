using Aura.Application.Ports;
using Aura.Infrastructure.Embedding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.Infrastructure;

public class EmbeddingDependencyInjectionTests
{
    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmbeddingProvider:Endpoint"] = "https://test.openai.azure.com",
                ["EmbeddingProvider:DeploymentName"] = "text-embedding-ada-002",
                ["EmbeddingProvider:ApiKey"] = "test-key"
            })
            .Build();

    [Fact]
    public void AddMeaiEmbeddingProvider_RegistersIEmbeddingProvider()
    {
        var services = new ServiceCollection();
        services.AddMeaiEmbeddingProvider(CreateConfig());

        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(IEmbeddingProvider));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddMeaiEmbeddingProvider_OverridesExistingRegistration()
    {
        var services = new ServiceCollection();
        // Simulate old registration from AddQdrantSemanticIndex
        services.AddSingleton<IEmbeddingProvider>(_ =>
            throw new InvalidOperationException("Old provider should be overridden"));
        services.AddMeaiEmbeddingProvider(CreateConfig());

        // MEAI registration should be the LAST one — .NET DI resolves the last registration
        var descriptors = services
            .Where(d => d.ServiceType == typeof(IEmbeddingProvider))
            .ToList();
        Assert.True(descriptors.Count >= 2, "Both registrations should exist");
        // Last registration wins in .NET DI
        var last = descriptors[^1];
        Assert.Equal(ServiceLifetime.Singleton, last.Lifetime);
    }

    [Fact]
    public void AddMeaiEmbeddingProvider_NullServices_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DependencyInjection.AddMeaiEmbeddingProvider(null!, CreateConfig()));
    }

    [Fact]
    public void AddMeaiEmbeddingProvider_NullConfiguration_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddMeaiEmbeddingProvider(null!));
    }

    [Fact]
    public void AddMeaiEmbeddingProvider_ResolvesFullPipeline()
    {
        var services = new ServiceCollection();
        services.AddMeaiEmbeddingProvider(CreateConfig());
        using var provider = services.BuildServiceProvider();

        var embedder = provider.GetRequiredService<IEmbeddingProvider>();

        Assert.NotNull(embedder);
        Assert.IsType<MeaiEmbeddingProvider>(embedder);
    }

    [Fact]
    public void AddMeaiEmbeddingProvider_ResolvesFullPipeline_WithCustomConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmbeddingProvider:Endpoint"] = "https://custom.openai.azure.com",
                ["EmbeddingProvider:DeploymentName"] = "custom-model",
                ["EmbeddingProvider:ApiKey"] = "custom-key",
                ["EmbeddingProvider:MaxRetries"] = "5",
                ["EmbeddingProvider:TimeoutSeconds"] = "60"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddMeaiEmbeddingProvider(config);
        using var provider = services.BuildServiceProvider();

        var embedder = provider.GetRequiredService<IEmbeddingProvider>();

        Assert.NotNull(embedder);
        Assert.IsType<MeaiEmbeddingProvider>(embedder);
    }
}
