using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Aura.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Aura.UnitTests.Infrastructure;

/// <summary>
/// Tests for the unified <see cref="Aura.Infrastructure.DependencyInjection.AddAuraInfrastructure"/> method.
/// Verifies that all adapter services resolve correctly from a single entry point.
/// </summary>
public class InfrastructureDependencyInjectionTests
{
    private static IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
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

    private static IHostEnvironment CreateDevEnvironment()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Development);
        return env;
    }

    [Fact]
    public void AddAuraInfrastructure_ResolvesIEmbeddingProvider()
    {
        var services = new ServiceCollection();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());
        using var provider = services.BuildServiceProvider();

        var embedding = provider.GetRequiredService<IEmbeddingProvider>();

        Assert.NotNull(embedding);
    }

    [Fact]
    public void AddAuraInfrastructure_ResolvesISemanticIndexWriter()
    {
        var services = new ServiceCollection();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var writer = scope.ServiceProvider.GetRequiredService<ISemanticIndexWriter>();

        Assert.NotNull(writer);
    }

    [Fact]
    public void AddAuraInfrastructure_ResolvesISemanticContextRetriever()
    {
        var services = new ServiceCollection();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var retriever = scope.ServiceProvider.GetRequiredService<ISemanticContextRetriever>();

        Assert.NotNull(retriever);
    }

    [Fact]
    public void AddAuraInfrastructure_ResolvesISemanticOutboxRepository()
    {
        var services = new ServiceCollection();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());
        using var provider = services.BuildServiceProvider();

        var repo = provider.GetRequiredService<ISemanticOutboxRepository>();

        Assert.NotNull(repo);
    }

    [Fact]
    public void AddAuraInfrastructure_RegistersTeamsConnectorAdapter_AsIConnectorAdapter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());
        using var provider = services.BuildServiceProvider();

        var adapters = provider.GetServices<IConnectorAdapter>().ToList();

        Assert.Contains(adapters, a => string.Equals(a.ConnectorName, "teams", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AddAuraInfrastructure_ResolvesIWorkItemStore_AndPersistsThroughPort()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var store = scope.ServiceProvider.GetRequiredService<IWorkItemStore>();
        var item = new WorkItem(
            externalId: "di-msg-1",
            title: "Persist from DI",
            source: "messages",
            sourceType: WorkItemSourceType.TeamsMessage,
            priority: WorkItemPriority.Medium,
            metadata: new Dictionary<string, string>());

        var result = await store.SaveAsync(item, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public void AddAuraInfrastructure_IWorkItemBuffer_IsIsolatedPerScope()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());
        using var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var buffer1 = scope1.ServiceProvider.GetRequiredService<IWorkItemBuffer>();
        var buffer2 = scope2.ServiceProvider.GetRequiredService<IWorkItemBuffer>();

        Assert.NotSame(buffer1, buffer2);
    }

    /// <summary>
    /// Negative DI test: proves that AddAuraInfrastructure alone does NOT register
    /// ISemanticChunkExtractor. This service belongs in the Application layer only.
    /// Spec: "application services MUST NOT be registered inside Aura.Infrastructure DI extensions."
    /// </summary>
    [Fact]
    public void AddAuraInfrastructure_DoesNotRegister_ISemanticChunkExtractor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());

        // Act
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISemanticChunkExtractor));

        // Assert: no registration for ISemanticChunkExtractor from infrastructure
        Assert.Null(descriptor);
    }

    /// <summary>
    /// Triangulation: proves that AddAuraInfrastructure does register infrastructure
    /// services (positive) but runtime resolution of ISemanticChunkExtractor throws.
    /// This confirms the negative case is behavioral, not just descriptor-level.
    /// </summary>
    [Fact]
    public void AddAuraInfrastructure_ResolvingISemanticChunkExtractor_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAuraInfrastructure(CreateConfig(), CreateDevEnvironment());
        using var provider = services.BuildServiceProvider();

        // Act & Assert: attempting to resolve throws because it's not registered
        Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<ISemanticChunkExtractor>());
    }
}
