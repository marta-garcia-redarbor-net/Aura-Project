using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;

/// <summary>
/// Qdrant implementation of <see cref="ISemanticContextRetriever"/>.
/// Searches Qdrant for semantically relevant chunks and applies orphan-chunk discard.
/// </summary>
internal sealed class QdrantSemanticContextAdapter : ISemanticContextRetriever
{
    private readonly QdrantClient _client;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly QdrantOptions _options;

    /// <summary>
    /// Optional delegate to check if a canonical source still exists.
    /// When null, no orphan filtering is applied (pass-through mode for V1).
    /// Wire this to the canonical store validator when the outbox/persistence is available.
    /// </summary>
    private readonly Func<string, CancellationToken, Task<bool>>? _canonicalSourceExists;

    public QdrantSemanticContextAdapter(
        QdrantClient client,
        IEmbeddingProvider embeddingProvider,
        IOptions<QdrantOptions> options,
        Func<string, CancellationToken, Task<bool>>? canonicalSourceExists = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _embeddingProvider = embeddingProvider ?? throw new ArgumentNullException(nameof(embeddingProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _canonicalSourceExists = canonicalSourceExists;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScoredSemanticChunk>> RetrieveAsync(SemanticQuery query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);

        var embedding = await _embeddingProvider.GenerateEmbeddingAsync(query.Text, ct);

        var collectionsToSearch = query.Collection.HasValue
            ? [_options.GetCollectionName(query.Collection.Value)]
            : Enum.GetValues<SemanticCollectionType>()
                .Select(t => _options.GetCollectionName(t))
                .ToArray();

        var results = new List<ScoredSemanticChunk>();

        foreach (var collectionName in collectionsToSearch)
        {
            if (!await _client.CollectionExistsAsync(collectionName, ct))
                continue;

            var searchResults = await _client.SearchAsync(
                collectionName,
                embedding.ToArray(),
                limit: (ulong)query.TopK,
                payloadSelector: new WithPayloadSelector { Enable = true },
                cancellationToken: ct);

            foreach (var scored in searchResults)
            {
                try
                {
                    var scoredChunk = QdrantPointMapper.ToScoredSemanticChunk(
                        scored.Id, scored.Payload, scored.Score);

                    // Apply tag filters if specified
                    if (query.TagFilters.Count > 0 && !MatchesTags(scoredChunk.Chunk, query.TagFilters))
                        continue;

                    results.Add(scoredChunk);
                }
                catch
                {
                    // Gracefully skip malformed payloads — treat as orphan
                    continue;
                }
            }
        }

        // Apply orphan-chunk discard if a canonical source validator is wired
        if (_canonicalSourceExists is not null)
        {
            results = await FilterOrphansAsync(results, ct);
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(query.TopK)
            .ToList();
    }

    /// <summary>
    /// Filters out orphan chunks whose canonical source no longer exists.
    /// Per spec: discard silently, no fatal errors.
    /// </summary>
    private async Task<List<ScoredSemanticChunk>> FilterOrphansAsync(
        List<ScoredSemanticChunk> chunks, CancellationToken ct)
    {
        var validated = new List<ScoredSemanticChunk>(chunks.Count);
        foreach (var chunk in chunks)
        {
            try
            {
                if (await _canonicalSourceExists!(chunk.Chunk.CanonicalSourceId, ct))
                    validated.Add(chunk);
            }
            catch
            {
                // Graceful recovery per spec — skip on error
            }
        }
        return validated;
    }

    /// <summary>
    /// Checks whether a chunk contains all required tag filters.
    /// </summary>
    internal static bool MatchesTags(SemanticChunk chunk, IReadOnlyList<DomainTag> tagFilters)
    {
        return tagFilters.All(required =>
            chunk.Tags.Any(t => t.Key == required.Key && t.Value == required.Value));
    }
}
