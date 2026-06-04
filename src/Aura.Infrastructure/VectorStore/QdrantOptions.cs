using Aura.Domain.SemanticIndex.Enums;

namespace Aura.Infrastructure.VectorStore;

/// <summary>
/// Configuration POCO for Qdrant vector store connection and collection mapping.
/// Bound from the "Qdrant" configuration section.
/// </summary>
public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    /// <summary>Qdrant server hostname.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Qdrant gRPC port.</summary>
    public int GrpcPort { get; set; } = 6334;

    /// <summary>Optional API key for authenticated clusters.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Collection name for stable project knowledge.</summary>
    public string ProjectKnowledgeCollection { get; set; } = "aura_project_knowledge";

    /// <summary>Collection name for fast-moving activity memory.</summary>
    public string ActivityMemoryCollection { get; set; } = "aura_activity_memory";

    /// <summary>Embedding vector dimension. Must match the embedding provider output.</summary>
    public int VectorSize { get; set; } = 1536;

    // ── Azure OpenAI embedding configuration (V1 — minimal) ─────────

    /// <summary>Azure OpenAI endpoint (e.g., "https://my-resource.openai.azure.com/").</summary>
    public string? AzureOpenAiEndpoint { get; set; }

    /// <summary>Azure OpenAI API key.</summary>
    public string? AzureOpenAiApiKey { get; set; }

    /// <summary>Azure OpenAI deployment name for embeddings.</summary>
    public string? AzureOpenAiDeployment { get; set; }

    /// <summary>Azure OpenAI API version.</summary>
    public string? AzureOpenAiApiVersion { get; set; }

    /// <summary>
    /// Maps a domain collection type to the configured Qdrant collection name.
    /// </summary>
    public string GetCollectionName(SemanticCollectionType type) => type switch
    {
        SemanticCollectionType.ProjectKnowledge => ProjectKnowledgeCollection,
        SemanticCollectionType.ActivityMemory => ActivityMemoryCollection,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown collection type.")
    };
}
