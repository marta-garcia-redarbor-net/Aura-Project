using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aura.Infrastructure.Adapters.Demo;

/// <summary>
/// Fallback implementation of <see cref="ISemanticContextRetriever"/> that returns empty results.
/// Used when Qdrant is unavailable or demo mode is active — graceful degradation without throwing.
/// </summary>
public sealed class QdrantFallbackSemanticContextRetriever : ISemanticContextRetriever
{
    private readonly ILogger<QdrantFallbackSemanticContextRetriever> _logger;

    public QdrantFallbackSemanticContextRetriever()
        : this(NullLogger<QdrantFallbackSemanticContextRetriever>.Instance)
    {
    }

    public QdrantFallbackSemanticContextRetriever(ILogger<QdrantFallbackSemanticContextRetriever> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ScoredSemanticChunk>> RetrieveAsync(SemanticQuery query, CancellationToken ct)
    {
        _logger.LogWarning("Qdrant fallback active — returning empty results for query: {QueryText}", query.Text);
        return Task.FromResult<IReadOnlyList<ScoredSemanticChunk>>(Array.Empty<ScoredSemanticChunk>());
    }
}
