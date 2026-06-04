using Aura.Domain.SemanticIndex.ValueObjects;

namespace Aura.Application.Models;

/// <summary>
/// Pairs a semantic chunk with its pre-computed embedding vector.
/// Used to pass enriched chunks to the writer — the writer persists, it does NOT embed.
/// </summary>
public sealed record EmbeddedSemanticChunk
{
    /// <summary>The semantic chunk with domain metadata.</summary>
    public required SemanticChunk Chunk { get; init; }

    /// <summary>Pre-computed embedding vector for the chunk content.</summary>
    public required ReadOnlyMemory<float> Embedding { get; init; }
}
