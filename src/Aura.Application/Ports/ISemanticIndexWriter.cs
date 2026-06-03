using Aura.Domain.SemanticIndex.ValueObjects;

namespace Aura.Application.Ports;

/// <summary>
/// Port for writing derived semantic chunks to the index.
/// Implementation lives in Infrastructure — never reference SDK types here.
/// </summary>
public interface ISemanticIndexWriter
{
    /// <summary>
    /// Writes a batch of semantic chunks to the appropriate collection.
    /// </summary>
    Task WriteAsync(IReadOnlyList<SemanticChunk> chunks, CancellationToken ct);

    /// <summary>
    /// Deletes all chunks derived from the given canonical source.
    /// </summary>
    Task DeleteByCanonicalIdAsync(string canonicalSourceId, CancellationToken ct);
}
