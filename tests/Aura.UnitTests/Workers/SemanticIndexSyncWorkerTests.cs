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
        _embedder.GenerateEmbeddingsAsync(
                Arg.Is<IReadOnlyList<string>>(t => t.Count == 1 && t[0] == chunk.Content),
                Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyMemory<float>> { TestEmbedding });

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
        _embedder.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Embedding service down"));

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        // Embedding failure marks the entry as failed (accumulation error path)
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
    public async Task ProcessBatchAsync_MultipleEntries_AccumulatesChunksThenWritesPerEntry()
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
        _embedder.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var texts = callInfo.ArgAt<IReadOnlyList<string>>(0);
                return texts.Select(_ => TestEmbedding).ToList() as IReadOnlyList<ReadOnlyMemory<float>>;
            });

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        // Embedding accumulated: single call with both chunks
        await _embedder.Received(1).GenerateEmbeddingsAsync(
            Arg.Is<IReadOnlyList<string>>(t => t.Count == 2),
            Arg.Any<CancellationToken>());

        // Write per entry (isolation preserved)
        await _writer.Received(2).WriteAsync(
            Arg.Any<IReadOnlyList<EmbeddedSemanticChunk>>(), Arg.Any<CancellationToken>());
        await _outbox.Received(2).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => e.Processed),
            Arg.Any<CancellationToken>());
    }

    // === Batch embedding migration tests (PR 2) ===

    [Fact]
    public async Task ProcessBatchAsync_UsesBatchEmbeddingApi_ForAllChunksInEntry()
    {
        var entry = CreateOutboxEntry("src-batch", "batch content");
        var chunk1 = CreateChunk("src-batch", "first chunk");
        var chunk2 = CreateChunk("src-batch", "second chunk");

        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry> { entry });
        _extractor.ExtractAsync(entry.CanonicalSourceId, entry.Content, entry.Collection, Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk1, chunk2 });
        _embedder.GenerateEmbeddingsAsync(
                Arg.Is<IReadOnlyList<string>>(texts => texts.Count == 2
                    && texts[0] == "first chunk" && texts[1] == "second chunk"),
                Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyMemory<float>>
            {
                new(new[] { 0.1f, 0.2f }),
                new(new[] { 0.3f, 0.4f })
            });

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        // Batch API should be called exactly once with both texts
        await _embedder.Received(1).GenerateEmbeddingsAsync(
            Arg.Is<IReadOnlyList<string>>(texts => texts.Count == 2),
            Arg.Any<CancellationToken>());

        // Single-text API should NOT be called
        await _embedder.DidNotReceive().GenerateEmbeddingAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());

        // Writer receives enriched chunks with correct embeddings zipped
        await _writer.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<EmbeddedSemanticChunk>>(enriched =>
                enriched.Count == 2
                && enriched[0].Chunk.Id == chunk1.Id
                && enriched[1].Chunk.Id == chunk2.Id
                && enriched[0].Embedding.Length == 2
                && enriched[1].Embedding.Length == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_SingleChunkEntry_StillUsesBatchApi()
    {
        var entry = CreateOutboxEntry("src-single", "single content");
        var chunk = CreateChunk("src-single", "only chunk");

        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry> { entry });
        _extractor.ExtractAsync(entry.CanonicalSourceId, entry.Content, entry.Collection, Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk });
        _embedder.GenerateEmbeddingsAsync(
                Arg.Is<IReadOnlyList<string>>(texts => texts.Count == 1 && texts[0] == "only chunk"),
                Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyMemory<float>> { new(new[] { 0.5f, 0.6f }) });

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        await _embedder.Received(1).GenerateEmbeddingsAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
        await _embedder.DidNotReceive().GenerateEmbeddingAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());

        await _writer.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<EmbeddedSemanticChunk>>(enriched =>
                enriched.Count == 1 && enriched[0].Chunk.Id == chunk.Id),
            Arg.Any<CancellationToken>());
    }

    // === Cross-entry accumulation tests (spec: "Syncing new evidence in batches") ===

    [Fact]
    public async Task ProcessBatchAsync_AccumulatesChunksAcrossEntries_IntoSingleEmbeddingCall()
    {
        // GIVEN two entries, each producing one chunk
        var entry1 = CreateOutboxEntry("src-001", "content 1");
        var entry2 = CreateOutboxEntry("src-002", "content 2");
        var chunk1 = CreateChunk("src-001", "chunk from entry 1");
        var chunk2 = CreateChunk("src-002", "chunk from entry 2");

        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry> { entry1, entry2 });
        _extractor.ExtractAsync("src-001", "content 1", Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk1 });
        _extractor.ExtractAsync("src-002", "content 2", Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk2 });

        // Mock: embedder receives ALL chunks from BOTH entries in a single call
        _embedder.GenerateEmbeddingsAsync(
                Arg.Is<IReadOnlyList<string>>(texts =>
                    texts.Count == 2
                    && texts[0] == "chunk from entry 1"
                    && texts[1] == "chunk from entry 2"),
                Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyMemory<float>>
            {
                new(new[] { 0.1f, 0.2f }),
                new(new[] { 0.3f, 0.4f })
            });

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        // THEN embedder is called exactly ONCE with both chunks accumulated
        await _embedder.Received(1).GenerateEmbeddingsAsync(
            Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());

        // AND writer receives per-entry enriched chunks (write isolation preserved)
        // with correct chunk-to-embedding association
        await _writer.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<EmbeddedSemanticChunk>>(enriched =>
                enriched.Count == 1
                && enriched[0].Chunk.Id == chunk1.Id
                && enriched[0].Embedding.Length == 2),
            Arg.Any<CancellationToken>());
        await _writer.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<EmbeddedSemanticChunk>>(enriched =>
                enriched.Count == 1
                && enriched[0].Chunk.Id == chunk2.Id
                && enriched[0].Embedding.Length == 2),
            Arg.Any<CancellationToken>());

        // AND both entries are marked processed
        await _outbox.Received(1).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => e.Id == entry1.Id && e.Processed),
            Arg.Any<CancellationToken>());
        await _outbox.Received(1).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => e.Id == entry2.Id && e.Processed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_MultipleEntriesMultipleChunks_AccumulatesAllIntoOneEmbeddingCall()
    {
        // GIVEN 3 entries: first has 2 chunks, second has 1, third has 0 (empty extraction)
        var entry1 = CreateOutboxEntry("src-001", "content 1");
        var entry2 = CreateOutboxEntry("src-002", "content 2");
        var entry3 = CreateOutboxEntry("src-003", "content 3");
        var chunk1a = CreateChunk("src-001", "chunk 1a");
        var chunk1b = CreateChunk("src-001", "chunk 1b");
        var chunk2 = CreateChunk("src-002", "chunk 2");

        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry> { entry1, entry2, entry3 });
        _extractor.ExtractAsync("src-001", "content 1", Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk1a, chunk1b });
        _extractor.ExtractAsync("src-002", "content 2", Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk2 });
        _extractor.ExtractAsync("src-003", "content 3", Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk>());

        _embedder.GenerateEmbeddingsAsync(
                Arg.Is<IReadOnlyList<string>>(texts =>
                    texts.Count == 3
                    && texts[0] == "chunk 1a"
                    && texts[1] == "chunk 1b"
                    && texts[2] == "chunk 2"),
                Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyMemory<float>>
            {
                new(new[] { 0.1f }),
                new(new[] { 0.2f }),
                new(new[] { 0.3f })
            });

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        // THEN embedder is called exactly ONCE with all 3 chunks accumulated
        await _embedder.Received(1).GenerateEmbeddingsAsync(
            Arg.Is<IReadOnlyList<string>>(t => t.Count == 3),
            Arg.Any<CancellationToken>());

        // AND entry1 gets 2 enriched chunks with correct embeddings
        await _writer.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<EmbeddedSemanticChunk>>(enriched =>
                enriched.Count == 2
                && enriched[0].Chunk.Id == chunk1a.Id
                && enriched[1].Chunk.Id == chunk1b.Id),
            Arg.Any<CancellationToken>());

        // AND entry2 gets 1 enriched chunk
        await _writer.Received(1).WriteAsync(
            Arg.Is<IReadOnlyList<EmbeddedSemanticChunk>>(enriched =>
                enriched.Count == 1
                && enriched[0].Chunk.Id == chunk2.Id),
            Arg.Any<CancellationToken>());

        // AND all 3 entries are marked processed (including entry3 with no chunks)
        await _outbox.Received(3).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => e.Processed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_ExtractionFailsForOneEntry_OthersStillAccumulateAndEmbed()
    {
        // GIVEN 3 entries: entry2's extraction throws
        var entry1 = CreateOutboxEntry("src-001", "content 1");
        var entry2 = CreateOutboxEntry("src-002", "content 2");
        var entry3 = CreateOutboxEntry("src-003", "content 3");
        var chunk1 = CreateChunk("src-001", "chunk 1");
        var chunk3 = CreateChunk("src-003", "chunk 3");

        _outbox.FetchPendingAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticOutboxEntry> { entry1, entry2, entry3 });
        _extractor.ExtractAsync("src-001", "content 1", Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk1 });
        _extractor.ExtractAsync("src-002", "content 2", Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("extraction failed"));
        _extractor.ExtractAsync("src-003", "content 3", Arg.Any<SemanticCollectionType>(), Arg.Any<CancellationToken>())
            .Returns(new List<SemanticChunk> { chunk3 });

        _embedder.GenerateEmbeddingsAsync(
                Arg.Is<IReadOnlyList<string>>(texts =>
                    texts.Count == 2
                    && texts[0] == "chunk 1"
                    && texts[1] == "chunk 3"),
                Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyMemory<float>>
            {
                new(new[] { 0.1f }),
                new(new[] { 0.3f })
            });

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        // THEN embedder is called ONCE with chunks from entry1 + entry3 (entry2 excluded)
        await _embedder.Received(1).GenerateEmbeddingsAsync(
            Arg.Is<IReadOnlyList<string>>(t => t.Count == 2),
            Arg.Any<CancellationToken>());

        // AND entry2 is marked failed (extraction error)
        await _outbox.Received(1).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => e.Id == entry2.Id && !e.Processed && e.Error != null),
            Arg.Any<CancellationToken>());

        // AND entry1 and entry3 are marked processed
        await _outbox.Received(1).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => e.Id == entry1.Id && e.Processed),
            Arg.Any<CancellationToken>());
        await _outbox.Received(1).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => e.Id == entry3.Id && e.Processed),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_EmbeddingFailsOnAccumulatedBatch_AllChunkedEntriesFail()
    {
        // GIVEN 2 entries with chunks, embedding call throws
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
        _embedder.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Embedding service down"));

        var worker = CreateWorker();
        await worker.ProcessBatchAsync(CancellationToken.None);

        // THEN writer is never called (embedding failed before write phase)
        await _writer.DidNotReceive().WriteAsync(
            Arg.Any<IReadOnlyList<EmbeddedSemanticChunk>>(), Arg.Any<CancellationToken>());

        // AND both entries are marked failed with the embedding error
        await _outbox.Received(1).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => e.Id == entry1.Id && !e.Processed && e.Error!.Contains("Embedding service down")),
            Arg.Any<CancellationToken>());
        await _outbox.Received(1).UpdateAsync(
            Arg.Is<SemanticOutboxEntry>(e => e.Id == entry2.Id && !e.Processed && e.Error!.Contains("Embedding service down")),
            Arg.Any<CancellationToken>());
    }

    // === Scope lifetime verification ===

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
        _embedder.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ReadOnlyMemory<float>> { TestEmbedding });

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
