<<<<<<< Updated upstream
using Aura.Application.Ports;
using Aura.Infrastructure.Embedding;
using Aura.Infrastructure.VectorStore;
using Aura.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
=======
using Aura.Application;
using Aura.Application.Ports;
using Aura.Infrastructure;
using Aura.Infrastructure.Adapters.Embedding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
>>>>>>> Stashed changes

namespace Aura.IntegrationTests.Workers;

/// <summary>
<<<<<<< Updated upstream
/// Proves that the Workers host composes correctly using the same DI extension methods
/// as <c>Program.cs</c>. Resolves critical services from a manually-built ServiceCollection
=======
/// Proves that the Workers host composes correctly using the unified DI extension methods.
/// Resolves critical services from a manually-built ServiceCollection
>>>>>>> Stashed changes
/// to verify composition without needing external infrastructure (Qdrant, OpenAI).
/// </summary>
public class WorkersHostCompositionTests
{
    private static ServiceProvider BuildWorkerServiceProvider()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Qdrant:Host"] = "localhost",
                ["Qdrant:GrpcPort"] = "6334",
                ["Qdrant:VectorSize"] = "768",
                ["ConnectionStrings:SemanticOutbox"] = "Data Source=:memory:",
                ["EmbeddingProvider:Endpoint"] = "https://test.openai.azure.com",
                ["EmbeddingProvider:DeploymentName"] = "text-embedding-ada-002",
                ["EmbeddingProvider:ApiKey"] = "test-key"
            })
            .Build();

        var services = new ServiceCollection();

        // Add logging (required by hosted services)
        services.AddLogging();

        // Mirror the exact extension method calls from Workers/Program.cs
<<<<<<< Updated upstream
        services.AddQdrantSemanticIndex(config);
        services.AddMeaiEmbeddingProvider(config);
=======
        services.AddAuraApplication();
        services.AddAuraInfrastructure(config);
>>>>>>> Stashed changes

        return services.BuildServiceProvider();
    }

    [Fact]
    public void WorkerHost_ResolvesIEmbeddingProvider()
    {
        using var sp = BuildWorkerServiceProvider();

        var provider = sp.GetRequiredService<IEmbeddingProvider>();

        Assert.NotNull(provider);
        Assert.IsType<MeaiEmbeddingProvider>(provider);
    }

    [Fact]
    public void WorkerHost_ResolvesISemanticIndexWriter()
    {
        using var sp = BuildWorkerServiceProvider();
        using var scope = sp.CreateScope();

        var writer = scope.ServiceProvider.GetRequiredService<ISemanticIndexWriter>();

        Assert.NotNull(writer);
    }

    [Fact]
    public void WorkerHost_ResolvesISemanticOutboxRepository()
    {
        using var sp = BuildWorkerServiceProvider();

        var repo = sp.GetRequiredService<ISemanticOutboxRepository>();

        Assert.NotNull(repo);
    }

    [Fact]
    public void WorkerHost_ResolvesISemanticChunkExtractor()
    {
        using var sp = BuildWorkerServiceProvider();

        var extractor = sp.GetRequiredService<ISemanticChunkExtractor>();

        Assert.NotNull(extractor);
    }
}
