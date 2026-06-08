namespace Aura.Application.Ports;

/// <summary>
/// Port for generating vector embeddings from text.
/// Defers the embedding model choice (OpenAI, Azure OpenAI, local) to Infrastructure.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>
    /// Generates vector embeddings for a batch of text inputs.
    /// </summary>
    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts, CancellationToken ct);

    /// <summary>
    /// Generates a single vector embedding. Default implementation delegates to the batch method.
    /// Retained for backward compatibility with existing consumers (e.g. SemanticIndexSyncWorker V1).
    /// </summary>
    async Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken ct)
    {
        var results = await GenerateEmbeddingsAsync(new[] { text }, ct);
        return results[0];
    }
}
