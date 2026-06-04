using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Port for writing derived semantic chunks to the index.
/// Implementation lives in Infrastructure — never reference SDK types here.
/// Chunks arrive pre-enriched with embeddings — the writer only persists.
/// </summary>
public interface ISemanticIndexWriter
{
    /// <summary>
    /// Writes a batch of pre-enriched semantic chunks to the appropriate collection.
    /// Embeddings are already computed — the writer MUST NOT generate them.
    /// </summary>
    Task WriteAsync(IReadOnlyList<EmbeddedSemanticChunk> chunks, CancellationToken ct);

    /// <summary>
    /// Deletes all chunks derived from the given canonical source.
    /// </summary>
    Task DeleteByCanonicalIdAsync(string canonicalSourceId, CancellationToken ct);
}
