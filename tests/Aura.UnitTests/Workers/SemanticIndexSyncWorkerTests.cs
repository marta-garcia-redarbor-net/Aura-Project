using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;
using Aura.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aura.UnitTests.Workers;

public class SemanticIndexSyncWorkerTests
{
    private readonly ISemanticOutboxRepository _outbox = Substitute.For<ISemanticOutboxRepository>();
    private readonly ISemanticChunkExtractor _extractor = Substitute.For<ISemanticChunkExtractor>();
    private readonly IEmbeddingProvider _embedder = Substitute.For<IEmbeddingProvider>();
    private readonly ISemanticIndexWriter _writer = Substitute.For<ISemanticIndexWriter>();
    private readonly ILogger<SemanticIndexSyncWorker> _logger = Substitute.For<ILogger<SemanticIndexSyncWorker>>();

    private static readonly ReadOnlyMemory<float> TestEmbedding = new(new[] { 0.1f, 0.2f, 0.3f });

    /// <summary>
    /// Creates a worker that resolves scoped ISemanticIndexWriter from IServiceScopeFactory.
    /// The worker now receives IServiceScopeFactory instead of ISemanticIndexWriter directly.
    /// </summary>
    private SemanticIndexSyncWorker CreateWorker()
    {
        var scopeFactory = CreateScopeFactory();
        return new SemanticIndexSyncWorker(_outbox, _extractor, _embedder, scopeFactory, _logger);
    }

    /// <summary>
    /// Builds an IServiceScopeFactory that returns a scope containing the mock ISemanticIndexWriter.
    /// </summary>
    private IServiceScopeFactory CreateScopeFactory()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ISemanticIndexWriter)).Returns(_writer);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        return scopeFactory;
    }

    private static SemanticOutboxEntry CreateOutboxEntry(
        string canonicalSourceId = "src-001",
        string content = "Test evidence content",
        SemanticCollectionType collection = SemanticCollectionType.ProjectKnowledge)
    {
        return new SemanticOutboxEntry(
            Guid.NewGuid(), canonicalSourceId, content, collection, DateTimeOffset.UtcNow);
    }

    private static SemanticChunk CreateChunk(
        string canonicalSourceId = "src-001",
        string content = "chunk content")
    {
        return new SemanticChunk(
            Guid.NewGuid(), canonicalSourceId, content,
            SemanticCollectionType.ProjectKnowledge,
            new List<DomainTag> { new("type", "evidence") },
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ProcessBatchAsync_ExtractsChunksEmbedsAndWrites()
    {
        var entry = CreateOutboxEntry();
        var chunk = CreateChunk();

        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry> { entry });
        _extractor.ExtractAsync(entry.CanonicalSourceId, entry.Content, entry.Collection, Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk });
        _embedder.GenerateEmbeddingAsync(chunk.Content, Arg.Any<CancellationToken>())
            .Returns(TestEmbedding);

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        // Writer receives enriched chunks (EmbeddedSemanticChunk)
        await _writer.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<EmbeddedSemanticChunk>>(enriched =>
                enriched.Count == 1 &&
                enriched[0].Chunk.Id == chunk.Id &&
                enriched[0].Embedding.Length == 3),
            Arg.Any<CancellationToken>());
        await _outbox.Received(1).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => e.Processed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_EmptyOutbox_DoesNothing()
    {
        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry>());

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        await _extractor.DidNotReceive().ExtractAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>());
        await _writer.DidNotReceive().WriteAsync(
            Arg.Any<IReadOnlyList<EmbeddedSemanticChunk>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_ExtractorThrows_MarksEntryFailed()
    {
        var entry = CreateOutboxEntry();
        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry> { entry });
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Chunk extraction failed"));

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        await _outbox.Received(1).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => !e.Processed && e.Error != null),
            Arg.Any<CancellationToken>());
        await _writer.DidNotReceive().WriteAsync(
            Arg.Any<IReadOnlyList<EmbeddedSemanticChunk>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_EmbeddingThrows_MarksEntryFailed()
    {
        var entry = CreateOutboxEntry();
        var chunk = CreateChunk();

        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry> { entry });
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk });
        _embedder.GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Embedding service down"));

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        await _outbox.Received(1).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => !e.Processed && e.Error!.Contains("Embedding service down")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_ExtractorReturnsEmpty_SkipsWriteButMarksProcessed()
    {
        var entry = CreateOutboxEntry();
        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry> { entry });
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk>());

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        await _writer.DidNotReceive().WriteAsync(
            Arg.Any<IReadOnlyList<EmbeddedSemanticChunk>>(), Arg.Any<CancellationToken>());
        await _outbox.Received(1).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => e.Processed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_MultipleEntries_ProcessesEachIndependently()
    {
        var entry1 = CreateOutboxEntry("src-001", "content 1");
        var entry2 = CreateOutboxEntry("src-002", "content 2");
        var chunk1 = CreateChunk("src-001", "chunk 1");
        var chunk2 = CreateChunk("src-002", "chunk 2");

        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry> { entry1, entry2 });
        _extractor.ExtractAsync("src-001", "content 1", Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk1 });
        _extractor.ExtractAsync("src-002", "content 2", Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk2 });
        _embedder.GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TestEmbedding);

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        await _writer.Received(2).WriteAsync(
            Arg.Any<IReadOnlyList<EmbeddedSemanticChunk>>(), Arg.Any<CancellationToken>());
        await _outbox.Received(2).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => e.Processed),
            Arg.Any<CancellationToken>());
    }

    // === NEW TESTS: Scope lifetime verification ===

    [Fact]
    public async Task ProcessBatchAsync_CreatesScopePerBatch_AndDisposesIt()
    {
        var entry = CreateOutboxEntry();
        var chunk = CreateChunk();

        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry> { entry });
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk });
        _embedder.GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TestEmbedding);

        var scope = Substitute.For<IServiceScope>();
        var scopeServiceProvider = Substitute.For<IServiceProvider>();
        scopeServiceProvider.GetService(typeof(ISemanticIndexWriter)).Returns(_writer);
        scope.ServiceProvider.Returns(scopeServiceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var worker = new SemanticIndexSyncWorker(_outbox, _extractor, _embedder, scopeFactory, _logger);
        await worker.ProcessBatchAsync(CancellationToken.None);

        // Scope was created and disposed
        scopeFactory.Received(1).CreateScope();
        scope.Received(1).Dispose();
    }

    [Fact]
    public async Task ProcessBatchAsync_EmptyOutbox_DoesNotCreateScope()
    {
        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry>());

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var worker = new SemanticIndexSyncWorker(_outbox, _extractor, _embedder, scopeFactory, _logger);
        await worker.ProcessBatchAsync(CancellationToken.None);

        // No scope should be created when there's nothing to process
        scopeFactory.DidNotReceive().CreateScope();
    }

    [Fact]
    public async Task ProcessBatchAsync_ScopeDisposed_EvenWhenProcessingFails()
    {
        var entry = CreateOutboxEntry();
        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry> { entry });
        _extractor.ExtractAsync(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var scope = Substitute.For<IServiceScope>();
        var scopeServiceProvider = Substitute.For<IServiceProvider>();
        scopeServiceProvider.GetService(typeof(ISemanticIndexWriter)).Returns(_writer);
        scope.ServiceProvider.Returns(scopeServiceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var worker = new SemanticIndexSyncWorker(_outbox, _extractor, _embedder, scopeFactory, _logger);
        await worker.ProcessBatchAsync(CancellationToken.None);

        // Scope MUST be disposed even when entries fail
        scope.Received(1).Dispose();
    }
}
