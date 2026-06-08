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
public sealed class SemanticIndexSyncWorker : BackgroundService
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
    {
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SemanticIndexSyncWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in SemanticIndexSyncWorker polling loop");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }

        _logger.LogInformation("SemanticIndexSyncWorker stopped");
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

        _logger.LogDebug("Processing {Count} outbox entries", entries.Count);

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
                _logger.LogWarning(ex,
                    "Failed to extract chunks for outbox entry {EntryId} (canonical source {CanonicalSourceId})",
                    entry.Id, entry.CanonicalSourceId);
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
                _logger.LogError(ex, "Batch embedding failed for {ChunkCount} accumulated chunks", allChunks.Count);

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
                _logger.LogDebug("Processed outbox entry {EntryId} for canonical source {CanonicalSourceId}",
                    entry.Id, entry.CanonicalSourceId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to write enriched chunks for outbox entry {EntryId} (canonical source {CanonicalSourceId})",
                    entry.Id, entry.CanonicalSourceId);
                entry.MarkFailed(ex.Message);
                offset += chunks.Count;
            }

            await _outbox.UpdateAsync(entry, ct);
        }
    }
}
