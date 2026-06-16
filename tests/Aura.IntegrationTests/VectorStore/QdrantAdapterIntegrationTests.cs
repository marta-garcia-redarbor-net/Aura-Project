using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;
using Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;
using Microsoft.Extensions.Options;

namespace Aura.IntegrationTests.VectorStore;

/// <summary>
/// Integration tests for Qdrant adapters against a real Qdrant instance via Testcontainers.
/// Each test uses unique collection names to avoid cross-test interference.
/// </summary>
[Collection("Qdrant")]
public sealed class QdrantAdapterIntegrationTests
{
    private readonly QdrantFixture _fixture;
    private static readonly IEmbeddingProvider FakeEmbeddings = new DeterministicEmbeddingProvider();

    public QdrantAdapterIntegrationTests(QdrantFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task WriteAndRetrieve_Roundtrip_PreservesChunkData()
    {
        // Arrange
        var (writer, retriever, _) = CreateAdapters();

        var tags = new List<DomainTag> { new("repo", "aura"), new("lang", "csharp") };
        var chunk = CreateChunk("src-roundtrip", "Design patterns in C#",
            SemanticCollectionType.ProjectKnowledge, tags);
        var enriched = await EnrichAsync(chunk);

        // Act — Write
        await writer.WriteAsync([enriched], CancellationToken.None);

        // Act — Retrieve (same text → same embedding → cosine similarity = 1.0)
        var query = new SemanticQuery
        {
            Text = "Design patterns in C#",
            Collection = SemanticCollectionType.ProjectKnowledge
        };
        var results = await retriever.RetrieveAsync(query, CancellationToken.None);

        // Assert
        Assert.NotEmpty(results);
        var found = Assert.Single(results, r => r.Chunk.Id == chunk.Id);
        Assert.Equal(chunk.CanonicalSourceId, found.Chunk.CanonicalSourceId);
        Assert.Equal(chunk.Content, found.Chunk.Content);
        Assert.Equal(chunk.Collection, found.Chunk.Collection);
        Assert.Equal(2, found.Chunk.Tags.Count);
        Assert.Contains(found.Chunk.Tags, t => t.Key == "repo" && t.Value == "aura");
        Assert.Contains(found.Chunk.Tags, t => t.Key == "lang" && t.Value == "csharp");
    }

    [Fact]
    public async Task DeleteByCanonicalId_RemovesChunksFromCollection()
    {
        // Arrange
        var (writer, retriever, _) = CreateAdapters();

        var toDelete1 = CreateChunk("src-del", "First chunk to delete", SemanticCollectionType.ProjectKnowledge);
        var toDelete2 = CreateChunk("src-del", "Second chunk to delete", SemanticCollectionType.ProjectKnowledge);
        var toKeep = CreateChunk("src-keep", "This chunk stays", SemanticCollectionType.ProjectKnowledge);

        var enriched = await EnrichAllAsync([toDelete1, toDelete2, toKeep]);
        await writer.WriteAsync(enriched, CancellationToken.None);

        // Act
        await writer.DeleteByCanonicalIdAsync("src-del", CancellationToken.None);

        // Assert — search broadly, only toKeep should remain
        var query = new SemanticQuery
        {
            Text = "chunk",
            Collection = SemanticCollectionType.ProjectKnowledge,
            TopK = 50
        };
        var results = await retriever.RetrieveAsync(query, CancellationToken.None);

        Assert.DoesNotContain(results, r => r.Chunk.CanonicalSourceId == "src-del");
        Assert.Contains(results, r => r.Chunk.Id == toKeep.Id);
    }

    [Fact]
    public async Task OrphanDiscard_FiltersChunksWhoseSourceNoLongerExists()
    {
        // Arrange
        var (writer, _, options) = CreateAdapters();

        var validChunk = CreateChunk("src-valid", "Valid source chunk", SemanticCollectionType.ActivityMemory);
        var orphanChunk = CreateChunk("src-orphan", "Orphan source chunk", SemanticCollectionType.ActivityMemory);

        var enriched = await EnrichAllAsync([validChunk, orphanChunk]);
        await writer.WriteAsync(enriched, CancellationToken.None);

        // Retriever with orphan filter: only "src-valid" exists in canonical store
        Func<string, CancellationToken, Task<bool>> canonicalExists =
            (id, _) => Task.FromResult(id == "src-valid");

        var retriever = new QdrantSemanticContextAdapter(
            _fixture.Client, FakeEmbeddings, options, canonicalExists);

        // Act
        var query = new SemanticQuery
        {
            Text = "source chunk",
            Collection = SemanticCollectionType.ActivityMemory,
            TopK = 50
        };
        var results = await retriever.RetrieveAsync(query, CancellationToken.None);

        // Assert
        Assert.Contains(results, r => r.Chunk.Id == validChunk.Id);
        Assert.DoesNotContain(results, r => r.Chunk.Id == orphanChunk.Id);
    }

    [Fact]
    public async Task TagFilter_OnlyReturnsChunksMatchingAllTags()
    {
        // Arrange
        var (writer, retriever, _) = CreateAdapters();

        var alphaChunk = CreateChunk("src-tf-1", "Team alpha documentation",
            SemanticCollectionType.ProjectKnowledge, [new DomainTag("team", "alpha")]);
        var betaChunk = CreateChunk("src-tf-2", "Team beta documentation",
            SemanticCollectionType.ProjectKnowledge, [new DomainTag("team", "beta")]);

        var enriched = await EnrichAllAsync([alphaChunk, betaChunk]);
        await writer.WriteAsync(enriched, CancellationToken.None);

        // Act — filter for team:alpha only
        var query = new SemanticQuery
        {
            Text = "documentation",
            Collection = SemanticCollectionType.ProjectKnowledge,
            TagFilters = [new DomainTag("team", "alpha")]
        };
        var results = await retriever.RetrieveAsync(query, CancellationToken.None);

        // Assert
        Assert.Contains(results, r => r.Chunk.Id == alphaChunk.Id);
        Assert.DoesNotContain(results, r => r.Chunk.Id == betaChunk.Id);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates adapters with unique collection names per invocation to isolate tests.
    /// </summary>
    private (QdrantSemanticIndexAdapter writer, QdrantSemanticContextAdapter retriever, IOptions<QdrantOptions> options)
        CreateAdapters()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var options = Options.Create(new QdrantOptions
        {
            Host = _fixture.Hostname,
            GrpcPort = _fixture.GrpcPort,
            VectorSize = 4,
            ProjectKnowledgeCollection = $"pk_{suffix}",
            ActivityMemoryCollection = $"am_{suffix}"
        });

        var writer = new QdrantSemanticIndexAdapter(_fixture.Client, options);
        var retriever = new QdrantSemanticContextAdapter(_fixture.Client, FakeEmbeddings, options);

        return (writer, retriever, options);
    }

    private static async Task<EmbeddedSemanticChunk> EnrichAsync(SemanticChunk chunk)
    {
        var embedding = await FakeEmbeddings.GenerateEmbeddingAsync(chunk.Content, CancellationToken.None);
        return new EmbeddedSemanticChunk { Chunk = chunk, Embedding = embedding };
    }

    private static async Task<List<EmbeddedSemanticChunk>> EnrichAllAsync(IReadOnlyList<SemanticChunk> chunks)
    {
        var result = new List<EmbeddedSemanticChunk>(chunks.Count);
        foreach (var chunk in chunks)
            result.Add(await EnrichAsync(chunk));
        return result;
    }

    private static SemanticChunk CreateChunk(
        string canonicalSourceId,
        string content,
        SemanticCollectionType collection,
        IReadOnlyList<DomainTag>? tags = null)
    {
        return new SemanticChunk(
            Guid.NewGuid(),
            canonicalSourceId,
            content,
            collection,
            tags ?? [],
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Deterministic embedding provider for integration tests.
    /// Returns a 4-dimensional vector derived from the text hash so that
    /// the same text always produces the same embedding within a process.
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
            // FNV-1a 32-bit — deterministic across runs unlike string.GetHashCode
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
