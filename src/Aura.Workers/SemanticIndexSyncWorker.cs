using Aura.Application.Models;
using Aura.Application.Ports;
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
    /// Exposed for unit testing — the TDD cycle exercises this directly.
    /// </summary>
    public async Task ProcessBatchAsync(CancellationToken ct)
    {
        var entries = await _outbox.FetchPendingAsync(BatchSize, ct);
        if (entries.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<ISemanticIndexWriter>();

        _logger.LogDebug("Processing {Count} outbox entries", entries.Count);

        foreach (var entry in entries)
        {
            try
            {
                var chunks = await _extractor.ExtractAsync(
                    entry.CanonicalSourceId, entry.Content, entry.Collection, ct);

                if (chunks.Count > 0)
                {
                    // Generate embeddings and create enriched chunks.
                    // Embedding ownership lives HERE — the writer only persists.
                    var enriched = new List<EmbeddedSemanticChunk>(chunks.Count);
                    foreach (var chunk in chunks)
                    {
                        var embedding = await _embedder.GenerateEmbeddingAsync(chunk.Content, ct);
                        enriched.Add(new EmbeddedSemanticChunk
                        {
                            Chunk = chunk,
                            Embedding = embedding
                        });
                    }

                    await writer.WriteAsync(enriched, ct);
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
                    "Failed to process outbox entry {EntryId} for canonical source {CanonicalSourceId}",
                    entry.Id, entry.CanonicalSourceId);
                entry.MarkFailed(ex.Message);
            }

            await _outbox.UpdateAsync(entry, ct);
        }
    }
}
