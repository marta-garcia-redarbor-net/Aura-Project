using Aura.Application;
using Aura.Application.Ports;
using Aura.Application.Services;
using Aura.Infrastructure.Adapters.Embedding;
using Aura.Infrastructure.Adapters.SemanticIndex;
using Aura.Infrastructure.Adapters.SemanticOutbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qdrant.Client;

namespace Aura.UnitTests.VectorStore;

public class DependencyInjectionTests
{
    private static (IServiceCollection Services, IConfiguration Config) CreateServicesWithConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Qdrant:Host"] = "test-host",
                ["Qdrant:GrpcPort"] = "6334",
                ["Qdrant:VectorSize"] = "768",
                ["ConnectionStrings:SemanticOutbox"] = "Data Source=:memory:",
                ["EmbeddingProvider:Endpoint"] = "https://test.openai.azure.com",
                ["EmbeddingProvider:DeploymentName"] = "text-embedding-ada-002",
                ["EmbeddingProvider:ApiKey"] = "test-key"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSemanticIndexAdapter(config);

        return (services, config);
    }

    [Fact]
    public void AddSemanticIndexAdapter_RegistersQdrantOptions()
    {
        var (services, _) = CreateServicesWithConfig();
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<QdrantOptions>>();

        Assert.Equal("test-host", options.Value.Host);
        Assert.Equal(6334, options.Value.GrpcPort);
        Assert.Equal(768, options.Value.VectorSize);
    }

    [Fact]
    public void AddSemanticIndexAdapter_RegistersQdrantClient_AsSingleton()
    {
        var (services, _) = CreateServicesWithConfig();

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(QdrantClient));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddSemanticIndexAdapter_ResolvesQdrantClient_WithConfiguredHost()
    {
        var (services, _) = CreateServicesWithConfig();
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<QdrantClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddSemanticIndexAdapter_ResolvesSemanticIndexWriter()
    {
        var (services, config) = CreateServicesWithConfig();
        services.AddEmbeddingAdapter(config);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var writer = scope.ServiceProvider.GetRequiredService<ISemanticIndexWriter>();

        Assert.NotNull(writer);
        Assert.IsType<QdrantSemanticIndexAdapter>(writer);
    }

    [Fact]
    public void AddSemanticIndexAdapter_ResolvesSemanticContextRetriever()
    {
        var (services, config) = CreateServicesWithConfig();
        services.AddEmbeddingAdapter(config);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var retriever = scope.ServiceProvider.GetRequiredService<ISemanticContextRetriever>();

        Assert.NotNull(retriever);
        Assert.IsType<QdrantSemanticContextAdapter>(retriever);
    }

    [Fact]
    public void AddSemanticIndexAdapter_RegistersSemanticIndexWriter_AsScoped()
    {
        var (services, _) = CreateServicesWithConfig();

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ISemanticIndexWriter));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddSemanticIndexAdapter_RegistersSemanticContextRetriever_AsScoped()
    {
        var (services, _) = CreateServicesWithConfig();

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ISemanticContextRetriever));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddSemanticIndexAdapter_NullServices_ThrowsArgumentNull()
    {
        var config = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() =>
            Aura.Infrastructure.Adapters.SemanticIndex.DependencyInjection.AddSemanticIndexAdapter(null!, config));
    }

    [Fact]
    public void AddSemanticIndexAdapter_NullConfiguration_ThrowsArgumentNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddSemanticIndexAdapter(null!));
    }

    // ── Cross-adapter DI composition ──────────────────────────────────

    [Fact]
    public void AddAuraApplication_ResolvesSemanticChunkExtractor()
    {
        var services = new ServiceCollection();
        services.AddAuraApplication();
        using var provider = services.BuildServiceProvider();

        var extractor = provider.GetRequiredService<ISemanticChunkExtractor>();

        Assert.NotNull(extractor);
        Assert.IsType<BasicSemanticChunkExtractor>(extractor);
    }

    [Fact]
    public void AddSemanticOutboxAdapter_ResolvesSemanticOutboxRepository()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SemanticOutbox"] = "Data Source=:memory:"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSemanticOutboxAdapter(config);
        using var provider = services.BuildServiceProvider();

        var repo = provider.GetRequiredService<ISemanticOutboxRepository>();

        Assert.NotNull(repo);
        Assert.IsType<SqliteSemanticOutboxRepository>(repo);
    }
}
