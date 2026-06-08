using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;
using Aura.Infrastructure.VectorStore;
using Aura.IntegrationTests.VectorStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aura.IntegrationTests.Embedding;

/// <summary>
/// E2E pipeline test: extract semantic chunks → generate batch embeddings → write to Qdrant → retrieve.
/// Validates the full data flow from design without outbox (outbox scope deferred).
/// Uses real Qdrant via Testcontainers and a deterministic embedding provider.
/// </summary>
[Collection("Qdrant")]
public class SemanticIndexPipelineTests
{
    private readonly QdrantFixture _fixture;

    public SemanticIndexPipelineTests(QdrantFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Full pipeline: extract chunks from content → batch-embed → write → retrieve by semantic query.
    /// Verifies data integrity end-to-end including embedding-based retrieval with relevance scores.
    /// </summary>
    [Fact]
    public async Task Pipeline_ExtractEmbedWriteRetrieve_ReturnsMatchingChunks()
    {
        // Arrange
        var (writer, retriever, embedder, extractor) = CreatePipelineComponents();

        const string canonicalSourceId = "evidence-001";
        const string content = "Clean Architecture separates concerns into layers. " +
                               "The domain layer contains business rules. " +
                               "Infrastructure adapters handle external dependencies.";

        // Step 1: Extract chunks
        var chunks = await extractor.ExtractAsync(
            canonicalSourceId, content, SemanticCollectionType.ProjectKnowledge, CancellationToken.None);

        Assert.NotEmpty(chunks);

        // Step 2: Batch-embed all chunks
        var texts = chunks.Select(c => c.Content).ToList();
        var embeddings = await embedder.GenerateEmbeddingsAsync(texts, CancellationToken.None);

        Assert.Equal(chunks.Count, embeddings.Count);

        // Step 3: Zip into enriched chunks and write
        var enriched = chunks
            .Zip(embeddings, (chunk, embedding) => new EmbeddedSemanticChunk
            {
                Chunk = chunk,
                Embedding = embedding
            })
            .ToList();

        await writer.WriteAsync(enriched, CancellationToken.None);

        // Step 4: Retrieve by semantic query
        var query = new SemanticQuery
        {
            Text = "business rules architecture",
            Collection = SemanticCollectionType.ProjectKnowledge,
            TopK = 10
        };
        var results = await retriever.RetrieveAsync(query, CancellationToken.None);

        // Assert — retrieved chunks should reference our canonical source
        Assert.NotEmpty(results);
        Assert.All(results, r =>
        {
            Assert.Equal(canonicalSourceId, r.Chunk.CanonicalSourceId);
            Assert.True(r.Score > 0, "Relevance score should be positive");
            Assert.False(string.IsNullOrEmpty(r.Chunk.Content), "Chunk content should not be empty");
        });
    }

    /// <summary>
    /// Validates batch embedding produces distinct vectors for distinct content,
    /// and retrieval ranks more relevant chunks higher.
    /// </summary>
    [Fact]
    public async Task Pipeline_DifferentContent_ProducesDifferentEmbeddingsAndRanks()
    {
        var (writer, retriever, embedder, _) = CreatePipelineComponents();

        // Create two chunks with very different content
        var relevantChunk = new SemanticChunk(
            Guid.NewGuid(), "src-relevant", "Polly resilience retry backoff timeout circuit breaker",
            SemanticCollectionType.ProjectKnowledge, [], DateTimeOffset.UtcNow);
        var irrelevantChunk = new SemanticChunk(
            Guid.NewGuid(), "src-irrelevant", "Recipe for chocolate cake with vanilla frosting",
            SemanticCollectionType.ProjectKnowledge, [], DateTimeOffset.UtcNow);

        // Batch-embed both
        var embeddings = await embedder.GenerateEmbeddingsAsync(
            new[] { relevantChunk.Content, irrelevantChunk.Content }, CancellationToken.None);

        // Verify distinct embeddings
        Assert.Equal(2, embeddings.Count);
        Assert.False(embeddings[0].Span.SequenceEqual(embeddings[1].Span),
            "Different content should produce different embeddings");

        // Write both
        var enriched = new List<EmbeddedSemanticChunk>
        {
            new() { Chunk = relevantChunk, Embedding = embeddings[0] },
            new() { Chunk = irrelevantChunk, Embedding = embeddings[1] }
        };
        await writer.WriteAsync(enriched, CancellationToken.None);

        // Query for resilience-related content
        var query = new SemanticQuery
        {
            Text = "Polly resilience retry",
            Collection = SemanticCollectionType.ProjectKnowledge,
            TopK = 2
        };
        var results = await retriever.RetrieveAsync(query, CancellationToken.None);

        // The relevant chunk should be returned first (higher score)
        Assert.NotEmpty(results);
        var topResult = results.OrderByDescending(r => r.Score).First();
        Assert.Equal("src-relevant", topResult.Chunk.CanonicalSourceId);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private (ISemanticIndexWriter writer, ISemanticContextRetriever retriever,
        IEmbeddingProvider embedder, ISemanticChunkExtractor extractor) CreatePipelineComponents()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var options = Options.Create(new QdrantOptions
        {
            Host = _fixture.Hostname,
            GrpcPort = _fixture.GrpcPort,
            VectorSize = 4,
            ProjectKnowledgeCollection = $"pk_pipe_{suffix}",
            ActivityMemoryCollection = $"am_pipe_{suffix}"
        });

        var embedder = new DeterministicEmbeddingProvider();
        var writer = new QdrantSemanticIndexAdapter(_fixture.Client, options);
        var retriever = new QdrantSemanticContextAdapter(_fixture.Client, embedder, options);
        var extractor = new Aura.Application.Services.BasicSemanticChunkExtractor();

        return (writer, retriever, embedder, extractor);
    }

    /// <summary>
    /// Deterministic embedding provider that produces 4-dimensional vectors from text hashes.
    /// Implements batch API so pipeline tests use the same code path as production.
    /// </summary>
    private sealed class DeterministicEmbeddingProvider : IEmbeddingProvider
    {
        public Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
            IReadOnlyList<string> texts, CancellationToken ct)
        {
            var results = texts.Select(text =>
            {
                var hash = StableHash(text);
                var embedding = new float[]
                {
                    (hash & 0xFF) / 255f,
                    ((hash >> 8) & 0xFF) / 255f,
                    ((hash >> 16) & 0xFF) / 255f,
                    ((hash >> 24) & 0xFF) / 255f
                };
                return (ReadOnlyMemory<float>)embedding;
            }).ToList();
            return Task.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(results);
        }

        private static uint StableHash(string input)
        {
            uint hash = 2166136261;
            foreach (var c in input)
            {
                hash ^= c;
                hash *= 16777619;
            }
            return hash;
        }
    }
}
