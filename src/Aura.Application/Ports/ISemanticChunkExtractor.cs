using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;

namespace Aura.Application.Ports;

/// <summary>
/// Port for domain-aware content chunking with PII stripping.
/// Chunking is Application-layer logic, not Infrastructure.
/// </summary>
public interface ISemanticChunkExtractor
{
    /// <summary>
    /// Extracts semantic chunks from raw content, applying PII stripping and tag assignment.
    /// </summary>
    Task<IReadOnlyList<SemanticChunk>> ExtractAsync(
        string canonicalSourceId,
        string content,
        SemanticCollectionType target,
        CancellationToken ct);
}
