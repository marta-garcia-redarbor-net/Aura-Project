namespace Aura.Infrastructure.Adapters.Embedding;

/// <summary>
/// Configuration for the MEAI-based embedding provider.
/// Bound from the "EmbeddingProvider" configuration section.
/// </summary>
public sealed class EmbeddingProviderOptions
{
    public const string SectionName = "EmbeddingProvider";

    /// <summary>Azure OpenAI endpoint URL.</summary>
    public required string Endpoint { get; set; }

    /// <summary>Azure OpenAI deployment/model name.</summary>
    public required string DeploymentName { get; set; }

    /// <summary>Maximum items per embedding API request.</summary>
    public int MaxBatchSize { get; set; } = 16;

    /// <summary>
    /// Maximum estimated tokens per batch request.
    /// Texts are split into sub-batches when cumulative estimated tokens exceed this limit.
    /// Token estimation uses chars/4 as a rough approximation.
    /// </summary>
    public int MaxTokensPerBatch { get; set; } = 8192;

    /// <summary>Timeout in seconds for a single embedding API call.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum retry attempts for transient failures (429, 503, timeout).</summary>
    public int MaxRetries { get; set; } = 3;
}
