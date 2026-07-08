using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aura.Infrastructure.Adapters.Demo;

/// <summary>
/// Fallback implementation of <see cref="ISemanticIndexWriter"/> that silently discards writes.
/// Used when Qdrant is unavailable or demo mode is active — graceful degradation without throwing.
/// </summary>
public sealed class QdrantFallbackSemanticIndexWriter : ISemanticIndexWriter
{
    private readonly ILogger<QdrantFallbackSemanticIndexWriter> _logger;

    public QdrantFallbackSemanticIndexWriter()
        : this(NullLogger<QdrantFallbackSemanticIndexWriter>.Instance)
    {
    }

    public QdrantFallbackSemanticIndexWriter(ILogger<QdrantFallbackSemanticIndexWriter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task WriteAsync(IReadOnlyList<EmbeddedSemanticChunk> chunks, CancellationToken ct)
    {
        _logger.LogWarning("Qdrant fallback active — discarding {ChunkCount} chunks", chunks.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteByCanonicalIdAsync(string canonicalSourceId, CancellationToken ct)
    {
        _logger.LogWarning("Qdrant fallback active — skipping delete for {CanonicalId}", canonicalSourceId);
        return Task.CompletedTask;
    }
}
