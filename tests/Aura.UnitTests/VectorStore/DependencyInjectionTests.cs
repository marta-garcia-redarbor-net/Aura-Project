using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Embedding;
using Aura.Infrastructure.Adapters.SemanticIndex;
using Aura.Application.Services;
using Aura.Infrastructure.Embedding;
using Aura.Infrastructure.Persistence;
using Aura.Infrastructure.VectorStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using VectorStoreDI = Aura.Infrastructure.VectorStore.DependencyInjection;

namespace Aura.UnitTests.VectorStore;

public class DependencyInjectionTests
{
<<<<<<< Updated upstream
    private static (IServiceCollection Services, IConfiguration Config) CreateServicesWithQdrant()
=======
    private static (IServiceCollection Services, IConfiguration Config) CreateServicesWithSemanticIndex()
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
        var (services, _) = CreateServicesWithQdrant();
=======
        var (services, _) = CreateServicesWithSemanticIndex();
>>>>>>> Stashed changes
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<QdrantOptions>>();

        Assert.Equal("test-host", options.Value.Host);
        Assert.Equal(6334, options.Value.GrpcPort);
        Assert.Equal(768, options.Value.VectorSize);
    }

    [Fact]
    public void AddSemanticIndexAdapter_RegistersQdrantClient_AsSingleton()
    {
<<<<<<< Updated upstream
        var (services, _) = CreateServicesWithQdrant();
=======
        var (services, _) = CreateServicesWithSemanticIndex();
>>>>>>> Stashed changes

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(QdrantClient));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddSemanticIndexAdapter_ResolvesQdrantClient_WithConfiguredHost()
    {
<<<<<<< Updated upstream
        var (services, _) = CreateServicesWithQdrant();
=======
        var (services, _) = CreateServicesWithSemanticIndex();
>>>>>>> Stashed changes
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<QdrantClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddSemanticIndexAdapter_ResolvesSemanticIndexWriter()
    {
<<<<<<< Updated upstream
        var (services, config) = CreateServicesWithQdrant();
        services.AddMeaiEmbeddingProvider(config);
=======
        var (services, config) = CreateServicesWithSemanticIndex();
        services.AddEmbeddingAdapter(config);
>>>>>>> Stashed changes
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var writer = scope.ServiceProvider.GetRequiredService<ISemanticIndexWriter>();

        Assert.NotNull(writer);
        Assert.IsType<QdrantSemanticIndexAdapter>(writer);
    }

    [Fact]
    public void AddSemanticIndexAdapter_ResolvesSemanticContextRetriever()
    {
<<<<<<< Updated upstream
        var (services, config) = CreateServicesWithQdrant();
        services.AddMeaiEmbeddingProvider(config);
=======
        var (services, config) = CreateServicesWithSemanticIndex();
        services.AddEmbeddingAdapter(config);
>>>>>>> Stashed changes
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var retriever = scope.ServiceProvider.GetRequiredService<ISemanticContextRetriever>();

        Assert.NotNull(retriever);
        Assert.IsType<QdrantSemanticContextAdapter>(retriever);
    }

    [Fact]
    public void AddSemanticIndexAdapter_RegistersSemanticIndexWriter_AsScoped()
    {
<<<<<<< Updated upstream
        var (services, _) = CreateServicesWithQdrant();
=======
        var (services, _) = CreateServicesWithSemanticIndex();
>>>>>>> Stashed changes

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ISemanticIndexWriter));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddSemanticIndexAdapter_RegistersSemanticContextRetriever_AsScoped()
    {
<<<<<<< Updated upstream
        var (services, _) = CreateServicesWithQdrant();
=======
        var (services, _) = CreateServicesWithSemanticIndex();
>>>>>>> Stashed changes

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
<<<<<<< Updated upstream
            VectorStoreDI.AddQdrantSemanticIndex(null!, config));
=======
            Aura.Infrastructure.Adapters.SemanticIndex.DependencyInjection.AddSemanticIndexAdapter(null!, config));
>>>>>>> Stashed changes
    }

    [Fact]
    public void AddSemanticIndexAdapter_NullConfiguration_ThrowsArgumentNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
<<<<<<< Updated upstream
            services.AddQdrantSemanticIndex(null!));
    }

    // ── Corrective patch DI registrations ──────────────────────

    [Fact]
    public void AddQdrantSemanticIndex_ResolvesSemanticChunkExtractor()
    {
        var (services, _) = CreateServicesWithQdrant();
        using var provider = services.BuildServiceProvider();

        var extractor = provider.GetRequiredService<ISemanticChunkExtractor>();

        Assert.NotNull(extractor);
        Assert.IsType<BasicSemanticChunkExtractor>(extractor);
    }

    [Fact]
    public void AddQdrantSemanticIndex_ResolvesSemanticOutboxRepository()
    {
        var (services, _) = CreateServicesWithQdrant();
        using var provider = services.BuildServiceProvider();

        var repo = provider.GetRequiredService<ISemanticOutboxRepository>();

        Assert.NotNull(repo);
        Assert.IsType<SqliteSemanticOutboxRepository>(repo);
=======
            services.AddSemanticIndexAdapter(null!));
>>>>>>> Stashed changes
    }
}
