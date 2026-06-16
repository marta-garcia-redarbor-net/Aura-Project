using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.SemanticIndex.Enums;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;

/// <summary>
/// Qdrant implementation of <see cref="ISemanticIndexWriter"/>.
/// Receives pre-enriched chunks (with embeddings) and persists them to Qdrant.
/// This adapter does NOT generate embeddings — that responsibility belongs to <see cref="IEmbeddingProvider"/>.
/// </summary>
internal sealed class QdrantSemanticIndexAdapter : ISemanticIndexWriter
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;

    public QdrantSemanticIndexAdapter(
        QdrantClient client,
        IOptions<QdrantOptions> options)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task WriteAsync(IReadOnlyList<EmbeddedSemanticChunk> chunks, CancellationToken ct)
    {
        if (chunks is null || chunks.Count == 0)
            return;

        // Group chunks by collection type so we upsert to the right Qdrant collection
        var grouped = chunks.GroupBy(c => c.Chunk.Collection);

        foreach (var group in grouped)
        {
            var collectionName = _options.GetCollectionName(group.Key);
            await EnsureCollectionExistsAsync(collectionName, ct);

            var points = new List<PointStruct>(group.Count());
            foreach (var enriched in group)
            {
                points.Add(QdrantPointMapper.ToPointStruct(enriched.Chunk, enriched.Embedding));
            }

            await _client.UpsertAsync(collectionName, points, cancellationToken: ct);
        }
    }

    /// <inheritdoc />
    public async Task DeleteByCanonicalIdAsync(string canonicalSourceId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(canonicalSourceId))
            throw new ArgumentException("Canonical source ID must not be null or empty.", nameof(canonicalSourceId));

        // Delete from both collections — the chunk could be in either
        foreach (SemanticCollectionType collectionType in Enum.GetValues<SemanticCollectionType>())
        {
            var collectionName = _options.GetCollectionName(collectionType);

            if (!await _client.CollectionExistsAsync(collectionName, ct))
                continue;

            await _client.DeleteAsync(
                collectionName,
                filter: Conditions.MatchKeyword("canonical_source_id", canonicalSourceId),
                cancellationToken: ct);
        }
    }

    private async Task EnsureCollectionExistsAsync(string collectionName, CancellationToken ct)
    {
        if (await _client.CollectionExistsAsync(collectionName, ct))
            return;

        await _client.CreateCollectionAsync(
            collectionName,
            new VectorParams
            {
                Size = (ulong)_options.VectorSize,
                Distance = Distance.Cosine
            },
            cancellationToken: ct);
    }
}
