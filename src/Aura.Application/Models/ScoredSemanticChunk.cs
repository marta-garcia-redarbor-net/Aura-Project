using Aura.Domain.SemanticIndex.ValueObjects;

namespace Aura.Application.Models;

/// <summary>
/// Result DTO pairing a semantic chunk with its relevance score.
/// </summary>
public sealed record ScoredSemanticChunk
{
    /// <summary>The retrieved semantic chunk.</summary>
    public required SemanticChunk Chunk { get; init; }

    /// <summary>Relevance score (0.0–1.0, higher = more relevant).</summary>
    public required double Score { get; init; }
}
