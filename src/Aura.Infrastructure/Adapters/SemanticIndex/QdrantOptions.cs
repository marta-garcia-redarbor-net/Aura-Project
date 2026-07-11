using Aura.Domain.SemanticIndex.Enums;

namespace Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;

/// <summary>
/// Configuration POCO for Qdrant vector store connection and collection mapping.
/// Bound from the "Qdrant" configuration section.
/// </summary>
public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    /// <summary>Qdrant server hostname.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Qdrant gRPC port (used for direct gRPC connections, e.g. local Docker).</summary>
    public int GrpcPort { get; set; } = 6334;

    /// <summary>Qdrant HTTP REST port. Used in ACA where the gRPC proxy is not supported.</summary>
    public int HttpPort { get; set; } = 6333;

    /// <summary>Optional API key for authenticated clusters.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Collection name for stable project knowledge.</summary>
    public string ProjectKnowledgeCollection { get; set; } = "aura_project_knowledge";

    /// <summary>Collection name for fast-moving activity memory.</summary>
    public string ActivityMemoryCollection { get; set; } = "aura_activity_memory";

    /// <summary>Embedding vector dimension. Must match the embedding provider output (nomic-embed-text => 768).</summary>
    public int VectorSize { get; set; } = 768;

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
