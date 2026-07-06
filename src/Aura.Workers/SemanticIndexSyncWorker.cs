using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.SemanticIndex.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aura.Workers;

/// <summary>
/// Background worker that polls the semantic outbox and syncs entries to the vector index.
/// Flow per entry: fetch → extract chunks → generate embeddings → write enriched chunks → mark processed.
/// Embedding generation happens here — the writer only persists pre-enriched chunks.
///
/// Uses IServiceScopeFactory to resolve scoped dependencies (ISemanticIndexWriter) per batch,
/// avoiding the hosted-service singleton → scoped service lifetime mismatch.
/// </summary>
public sealed partial class SemanticIndexSyncWorker : CorrelatedWorkerBase
{
    private readonly ISemanticOutboxRepository _outbox;
    private readonly ISemanticChunkExtractor _extractor;
    private readonly IEmbeddingProvider _embedder;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SemanticIndexSyncWorker> _logger;

    private const int BatchSize = 20;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

    public SemanticIndexSyncWorker(
        ISemanticOutboxRepository outbox,
        ISemanticChunkExtractor extractor,
        IEmbeddingProvider embedder,
        IServiceScopeFactory scopeFactory,
        ILogger<SemanticIndexSyncWorker> logger)
        : base(logger)
    {
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteCorrelatedAsync(string correlationId, CancellationToken stoppingToken)
    {
        try
        {
            await ProcessBatchAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            Log.UnhandledPollingError(_logger, ex);
        }

        await Task.Delay(PollingInterval, stoppingToken);
    }

    /// <summary>
    /// Processes a single batch of pending outbox entries.
    /// Creates a DI scope per batch to safely resolve scoped services (ISemanticIndexWriter).
    ///
    /// Accumulates semantic chunks across all entries before embedding, so the embedding
    /// provider receives a single batch call (spec: "Syncing new evidence in batches").
    /// Individual entries are still marked processed/failed independently.
    /// Exposed for unit testing — the TDD cycle exercises this directly.
    /// </summary>
    public async Task ProcessBatchAsync(CancellationToken ct)
    {
        var entries = await _outbox.FetchPendingAsync(BatchSize, ct);
        if (entries.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<ISemanticIndexWriter>();

        Log.ProcessingBatch(_logger, entries.Count);

        // Phase 1: Extract chunks per entry, tracking association.
        // Entries where extraction fails are marked immediately and excluded from embedding.
        var extractionResults = new List<(SemanticOutboxEntry Entry, IReadOnlyList<SemanticChunk> Chunks)>();

        foreach (var entry in entries)
        {
            try
            {
                var chunks = await _extractor.ExtractAsync(
                    entry.CanonicalSourceId, entry.Content, entry.Collection, ct);
                extractionResults.Add((entry, chunks));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.ExtractionFailed(_logger, ex, entry.Id, entry.CanonicalSourceId);
                entry.MarkFailed(ex.Message);
                await _outbox.UpdateAsync(entry, ct);
            }
        }

        // Phase 2: Accumulate all chunks across entries into a single embedding call.
        var allChunks = extractionResults
            .SelectMany(r => r.Chunks)
            .ToList();

        IReadOnlyList<ReadOnlyMemory<float>>? allEmbeddings = null;

        if (allChunks.Count > 0)
        {
            try
            {
                var allTexts = allChunks.Select(c => c.Content).ToList();
                allEmbeddings = await _embedder.GenerateEmbeddingsAsync(allTexts, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.BatchEmbeddingFailed(_logger, ex, allChunks.Count);

                // All entries with chunks fail when embedding fails.
                foreach (var (entry, chunks) in extractionResults)
                {
                    if (chunks.Count > 0)
                    {
                        entry.MarkFailed(ex.Message);
                    }
                    else
                    {
                        entry.MarkProcessed();
                    }

                    await _outbox.UpdateAsync(entry, ct);
                }

                return;
            }
        }

        // Phase 3: Distribute embeddings back to entries and write.
        var offset = 0;

        foreach (var (entry, chunks) in extractionResults)
        {
            try
            {
                if (chunks.Count > 0 && allEmbeddings is not null)
                {
                    var enriched = chunks
                        .Select((chunk, i) => new EmbeddedSemanticChunk
                        {
                            Chunk = chunk,
                            Embedding = allEmbeddings[offset + i]
                        })
                        .ToList();

                    await writer.WriteAsync(enriched, ct);
                    offset += chunks.Count;
                }

                entry.MarkProcessed();
                Log.EntryProcessed(_logger, entry.Id, entry.CanonicalSourceId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.WriteFailed(_logger, ex, entry.Id, entry.CanonicalSourceId);
                entry.MarkFailed(ex.Message);
                offset += chunks.Count;
            }

            await _outbox.UpdateAsync(entry, ct);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 3101, Level = LogLevel.Error,
            Message = "Unhandled error in SemanticIndexSyncWorker polling loop")]
        public static partial void UnhandledPollingError(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 3102, Level = LogLevel.Debug,
            Message = "Processing {Count} outbox entries")]
        public static partial void ProcessingBatch(ILogger logger, int count);

        [LoggerMessage(EventId = 3103, Level = LogLevel.Warning,
            Message = "Failed to extract chunks for outbox entry {EntryId} (canonical source {CanonicalSourceId})")]
        public static partial void ExtractionFailed(ILogger logger, Exception exception, Guid entryId, string canonicalSourceId);

        [LoggerMessage(EventId = 3104, Level = LogLevel.Error,
            Message = "Batch embedding failed for {ChunkCount} accumulated chunks")]
        public static partial void BatchEmbeddingFailed(ILogger logger, Exception exception, int chunkCount);

        [LoggerMessage(EventId = 3105, Level = LogLevel.Debug,
            Message = "Processed outbox entry {EntryId} for canonical source {CanonicalSourceId}")]
        public static partial void EntryProcessed(ILogger logger, Guid entryId, string canonicalSourceId);

        [LoggerMessage(EventId = 3106, Level = LogLevel.Warning,
            Message = "Failed to write enriched chunks for outbox entry {EntryId} (canonical source {CanonicalSourceId})")]
        public static partial void WriteFailed(ILogger logger, Exception exception, Guid entryId, string canonicalSourceId);
    }
}
