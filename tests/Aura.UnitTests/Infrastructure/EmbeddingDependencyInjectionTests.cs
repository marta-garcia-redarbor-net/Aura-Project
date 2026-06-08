using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Embedding;
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
    public void AddEmbeddingAdapter_RegistersIEmbeddingProvider()
    {
        var services = new ServiceCollection();
        services.AddEmbeddingAdapter(CreateConfig());

        var descriptor = services.LastOrDefault(d => d.ServiceType == typeof(IEmbeddingProvider));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddEmbeddingAdapter_OverridesExistingRegistration()
    {
        var services = new ServiceCollection();
        // Simulate old registration that should be overridden
        services.AddSingleton<IEmbeddingProvider>(_ =>
            throw new InvalidOperationException("Old provider should be overridden"));
        services.AddEmbeddingAdapter(CreateConfig());

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
    public void AddEmbeddingAdapter_NullServices_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DependencyInjection.AddEmbeddingAdapter(null!, CreateConfig()));
    }

    [Fact]
    public void AddEmbeddingAdapter_NullConfiguration_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddEmbeddingAdapter(null!));
    }

    [Fact]
    public void AddEmbeddingAdapter_ResolvesFullPipeline()
    {
        var services = new ServiceCollection();
        services.AddEmbeddingAdapter(CreateConfig());
        using var provider = services.BuildServiceProvider();

        var embedder = provider.GetRequiredService<IEmbeddingProvider>();

        Assert.NotNull(embedder);
        Assert.IsType<MeaiEmbeddingProvider>(embedder);
    }

    [Fact]
    public void AddEmbeddingAdapter_ResolvesFullPipeline_WithCustomConfig()
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
        services.AddEmbeddingAdapter(config);
        using var provider = services.BuildServiceProvider();

        var embedder = provider.GetRequiredService<IEmbeddingProvider>();

        Assert.NotNull(embedder);
        Assert.IsType<MeaiEmbeddingProvider>(embedder);
    }
}
