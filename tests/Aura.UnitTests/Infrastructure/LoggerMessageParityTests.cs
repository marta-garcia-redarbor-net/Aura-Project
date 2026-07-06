using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.Kernel;
using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;
using Aura.Domain.WorkItems;
using Aura.UnitTests.TestDoubles.Observability;
using Aura.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Aura.UnitTests.Infrastructure;

public class LoggerMessageParityTests
{
    [Fact]
    public async Task Worker_EmitsOriginalTemplateAndLevel()
    {
        var logger = new ScopeAwareTestLogger<Worker>();
        var worker = new Worker(logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(80));
        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(60);
        await worker.StopAsync(CancellationToken.None);

        var log = Assert.Single(logger.Entries.Where(e => e.EventId.Id == 3301));
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, log.Level);
        Assert.Contains("Worker running at:", log.Message, StringComparison.Ordinal);
        Assert.True(log.State.ContainsKey("time"));
    }

    [Fact]
    public async Task PluginRegistry_EmitsOriginalFailureTemplateAndParameters()
    {
        var logger = new ScopeAwareTestLogger<PluginRegistry>();
        var failingPlugin = Substitute.For<IPlugin>();
        failingPlugin.ExecuteAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("plugin boom"));

        var registry = new PluginRegistry(new[] { failingPlugin }, logger);
        var workItem = new WorkItem(
            externalId: "ext-42",
            title: "Plugin parity",
            source: "manual",
            sourceType: WorkItemSourceType.TeamsMessage,
            priority: WorkItemPriority.High,
            metadata: new Dictionary<string, string>(),
            correlationId: "corr-plugin-42",
            capturedAtUtc: DateTimeOffset.UtcNow);

        await registry.ExecuteAsync(workItem, CancellationToken.None);

        var log = Assert.Single(logger.Entries.Where(e => e.EventId.Id == 3401));
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Error, log.Level);
        Assert.Contains("Plugin", log.Message, StringComparison.Ordinal);
        Assert.Equal("ext-42", log.State["ExternalId"]?.ToString());
        Assert.Equal("corr-plugin-42", log.State["CorrelationId"]?.ToString());
        Assert.NotNull(log.Exception);
    }

    [Fact]
    public async Task SemanticIndexSyncWorker_EmitsOriginalBatchTemplateAndCountParameter()
    {
        var logger = new ScopeAwareTestLogger<SemanticIndexSyncWorker>();
        var outbox = Substitute.For<ISemanticOutboxRepository>();
        var extractor = Substitute.For<ISemanticChunkExtractor>();
        var embedder = Substitute.For<IEmbeddingProvider>();
        var writer = Substitute.For<ISemanticIndexWriter>();

        var entry = new SemanticOutboxEntry(
            Guid.NewGuid(),
            "src-logger-parity",
            "content",
            SemanticCollectionType.ProjectKnowledge,
            DateTimeOffset.UtcNow);

        var chunk = new SemanticChunk(
            Guid.NewGuid(),
            entry.CanonicalSourceId,
            "chunk",
            SemanticCollectionType.ProjectKnowledge,
            [new DomainTag("kind", "test")],
            DateTimeOffset.UtcNow);

        outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([entry]);
        extractor.ExtractAsync(entry.CanonicalSourceId, entry.Content, entry.Collection, Arg.Any<CancellationToken>())
            .Returns([chunk]);
        embedder.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns([new ReadOnlyMemory<float>([0.1f, 0.2f])]);

        var services = new ServiceCollection();
        services.AddSingleton(writer);
        await using var provider = services.BuildServiceProvider();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var worker = new SemanticIndexSyncWorker(outbox, extractor, embedder, scopeFactory, logger);
        await worker.ProcessBatchAsync(CancellationToken.None);

        var log = Assert.Single(logger.Entries.Where(e => e.EventId.Id == 3102));
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Debug, log.Level);
        Assert.Contains("Processing", log.Message, StringComparison.Ordinal);
        Assert.Equal("1", log.State["Count"]?.ToString());
    }

    [Fact]
    public async Task HelloKernelWorker_EmitsOriginalCompletionTemplateAndCorrelationParameter()
    {
        var logger = new ScopeAwareTestLogger<HelloKernelWorker>();
        var registry = Substitute.For<IPluginRegistry>();
        registry.ExecuteAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        var lifetime = Substitute.For<IHostApplicationLifetime>();

        var worker = new HelloKernelWorker(registry, lifetime, logger);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(60);
        await worker.StopAsync(CancellationToken.None);

        var log = Assert.Single(logger.Entries.Where(e => e.EventId.Id == 3202));
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, log.Level);
        Assert.Contains("HelloKernelWorker completed.", log.Message, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(log.State["CorrelationId"]?.ToString()));
    }
}
