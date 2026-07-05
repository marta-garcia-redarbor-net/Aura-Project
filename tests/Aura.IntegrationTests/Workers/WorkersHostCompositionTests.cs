using Aura.Application;
using Aura.Application.Ports;
using Aura.Infrastructure;
using Aura.Infrastructure.Adapters.Ingestion.Embedding;
using Aura.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Aura.IntegrationTests.Workers;

/// <summary>
/// Proves that the Workers host composes correctly using the unified DI extension methods.
/// Resolves critical services from a manually-built ServiceCollection
/// to verify composition without needing external infrastructure (Qdrant, OpenAI).
/// </summary>
public class WorkersHostCompositionTests
{
    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Aura.Workers.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static ServiceProvider BuildWorkerServiceProvider()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aura-workers-{Guid.NewGuid():N}.db");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Qdrant:Host"] = "localhost",
                ["Qdrant:GrpcPort"] = "6334",
                ["Qdrant:VectorSize"] = "768",
                ["ConnectionStrings:SemanticOutbox"] = "Data Source=:memory:",
                ["ConnectionStrings:Aura"] = $"Data Source={dbPath}",
                ["EmbeddingProvider:Endpoint"] = "https://test.openai.azure.com",
                ["EmbeddingProvider:DeploymentName"] = "text-embedding-ada-002",
                ["EmbeddingProvider:ApiKey"] = "test-key",
                ["MorningSummary:TimezoneId"] = "UTC",
                ["MorningSummary:TargetLocalTime"] = "09:00"
            })
            .Build();

        var services = new ServiceCollection();

        // Add logging (required by hosted services)
        services.AddLogging();

        // Mirror the exact extension method calls from Workers/Program.cs
        services.AddAuraApplication();
        services.AddAuraInfrastructure(config, new FakeHostEnvironment());
        services.AddHostedService<MorningSummarySchedulingWorker>();

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

    [Fact]
    public void WorkerHost_RegistersMorningSummarySchedulingWorker_AsHostedService()
    {
        using var sp = BuildWorkerServiceProvider();

        var hostedServices = sp.GetServices<IHostedService>().ToList();

        Assert.Contains(hostedServices, service => service is MorningSummarySchedulingWorker);
    }

    [Fact]
    public void WorkerHost_ResolvesInterruptionPolicyScoringDependencies()
    {
        using var sp = BuildWorkerServiceProvider();
        using var scope = sp.CreateScope();

        var scorer = scope.ServiceProvider.GetRequiredService<IPriorityScoringService>();
        var policyProvider = scope.ServiceProvider.GetRequiredService<IUserTriagePolicyProvider>();
        var engine = scope.ServiceProvider.GetRequiredService<IInterruptionPolicyEngine>();

        Assert.NotNull(scorer);
        Assert.NotNull(policyProvider);
        Assert.NotNull(engine);
    }
}
