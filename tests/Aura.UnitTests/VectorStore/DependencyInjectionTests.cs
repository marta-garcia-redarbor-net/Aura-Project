using Aura.Application.Ports;
using Aura.Application.Services;
using Aura.Infrastructure.Embedding;
using Aura.Infrastructure.Persistence;
using Aura.Infrastructure.VectorStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Qdrant.Client;
using VectorStoreDI = Aura.Infrastructure.VectorStore.DependencyInjection;

namespace Aura.UnitTests.VectorStore;

public class DependencyInjectionTests
{
    private static (IServiceCollection Services, IConfiguration Config) CreateServicesWithQdrant()
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
        services.AddQdrantSemanticIndex(config);

        return (services, config);
    }

    [Fact]
    public void AddQdrantSemanticIndex_RegistersQdrantOptions()
    {
        var (services, _) = CreateServicesWithQdrant();
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<QdrantOptions>>();

        Assert.Equal("test-host", options.Value.Host);
        Assert.Equal(6334, options.Value.GrpcPort);
        Assert.Equal(768, options.Value.VectorSize);
    }

    [Fact]
    public void AddQdrantSemanticIndex_RegistersQdrantClient_AsSingleton()
    {
        var (services, _) = CreateServicesWithQdrant();

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(QdrantClient));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddQdrantSemanticIndex_ResolvesQdrantClient_WithConfiguredHost()
    {
        var (services, _) = CreateServicesWithQdrant();
        using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<QdrantClient>();

        Assert.NotNull(client);
    }

    [Fact]
    public void AddQdrantSemanticIndex_ResolvesSemanticIndexWriter()
    {
        var (services, config) = CreateServicesWithQdrant();
        services.AddMeaiEmbeddingProvider(config);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var writer = scope.ServiceProvider.GetRequiredService<ISemanticIndexWriter>();

        Assert.NotNull(writer);
        Assert.IsType<QdrantSemanticIndexAdapter>(writer);
    }

    [Fact]
    public void AddQdrantSemanticIndex_ResolvesSemanticContextRetriever()
    {
        var (services, config) = CreateServicesWithQdrant();
        services.AddMeaiEmbeddingProvider(config);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var retriever = scope.ServiceProvider.GetRequiredService<ISemanticContextRetriever>();

        Assert.NotNull(retriever);
        Assert.IsType<QdrantSemanticContextAdapter>(retriever);
    }

    [Fact]
    public void AddQdrantSemanticIndex_RegistersSemanticIndexWriter_AsScoped()
    {
        var (services, _) = CreateServicesWithQdrant();

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ISemanticIndexWriter));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddQdrantSemanticIndex_RegistersSemanticContextRetriever_AsScoped()
    {
        var (services, _) = CreateServicesWithQdrant();

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(ISemanticContextRetriever));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddQdrantSemanticIndex_NullServices_ThrowsArgumentNull()
    {
        var config = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() =>
            VectorStoreDI.AddQdrantSemanticIndex(null!, config));
    }

    [Fact]
    public void AddQdrantSemanticIndex_NullConfiguration_ThrowsArgumentNull()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
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
    }
}
