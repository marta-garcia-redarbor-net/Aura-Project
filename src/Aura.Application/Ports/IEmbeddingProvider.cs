namespace Aura.Application.Ports;

/// <summary>
/// Port for generating vector embeddings from text.
/// Defers the embedding model choice (OpenAI, Azure OpenAI, local) to Infrastructure.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>
    /// Generates a vector embedding for the given text input.
    /// </summary>
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken ct);
}
