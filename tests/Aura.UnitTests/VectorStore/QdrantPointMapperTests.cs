using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;
using Aura.Infrastructure.VectorStore;

namespace Aura.UnitTests.VectorStore;

public class QdrantPointMapperTests
{
    private static SemanticChunk CreateChunk(
        Guid? id = null,
        string canonicalSourceId = "src-1",
        string content = "Test content",
        SemanticCollectionType collection = SemanticCollectionType.ProjectKnowledge,
        IReadOnlyList<DomainTag>? tags = null)
    {
        return new SemanticChunk(
            id ?? Guid.NewGuid(),
            canonicalSourceId,
            content,
            collection,
            tags ?? [new DomainTag("area", "auth")],
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ToPointStruct_SetsIdFromChunkGuid()
    {
        var chunkId = Guid.NewGuid();
        var chunk = CreateChunk(id: chunkId);
        var embedding = new ReadOnlyMemory<float>([0.1f, 0.2f, 0.3f]);

        var point = QdrantPointMapper.ToPointStruct(chunk, embedding);

        Assert.Equal(chunkId.ToString(), point.Id.Uuid);
    }

    [Fact]
    public void ToPointStruct_SetsVectorsFromEmbedding()
    {
        var chunk = CreateChunk();
        var embedding = new ReadOnlyMemory<float>([0.5f, 0.6f, 0.7f]);

        var point = QdrantPointMapper.ToPointStruct(chunk, embedding);

        Assert.NotNull(point.Vectors);
        Assert.NotNull(point.Vectors.Vector);
        Assert.Equal(3, point.Vectors.Vector.Data.Count);
        Assert.Equal(0.5f, point.Vectors.Vector.Data[0]);
        Assert.Equal(0.6f, point.Vectors.Vector.Data[1]);
        Assert.Equal(0.7f, point.Vectors.Vector.Data[2]);
    }

    [Fact]
    public void ToPointStruct_PreservesEmbeddingDimension_SingleElement()
    {
        var chunk = CreateChunk();
        var embedding = new ReadOnlyMemory<float>([0.99f]);

        var point = QdrantPointMapper.ToPointStruct(chunk, embedding);

        Assert.Equal(1, point.Vectors.Vector.Data.Count);
        Assert.Equal(0.99f, point.Vectors.Vector.Data[0]);
    }

    [Fact]
    public void ToPointStruct_StoresCanonicalSourceIdInPayload()
    {
        var chunk = CreateChunk(canonicalSourceId: "canonical-42");
        var embedding = new ReadOnlyMemory<float>([0.1f]);

        var point = QdrantPointMapper.ToPointStruct(chunk, embedding);

        Assert.Equal("canonical-42", point.Payload["canonical_source_id"].StringValue);
    }

    [Fact]
    public void ToPointStruct_StoresContentInPayload()
    {
        var chunk = CreateChunk(content: "Important decision");
        var embedding = new ReadOnlyMemory<float>([0.1f]);

        var point = QdrantPointMapper.ToPointStruct(chunk, embedding);

        Assert.Equal("Important decision", point.Payload["content"].StringValue);
    }

    [Fact]
    public void ToPointStruct_StoresCollectionTypeInPayload()
    {
        var chunk = CreateChunk(collection: SemanticCollectionType.ActivityMemory);
        var embedding = new ReadOnlyMemory<float>([0.1f]);

        var point = QdrantPointMapper.ToPointStruct(chunk, embedding);

        Assert.Equal("ActivityMemory", point.Payload["collection"].StringValue);
    }

    [Fact]
    public void ToPointStruct_StoresCreatedAtInPayload()
    {
        var chunk = CreateChunk();
        var embedding = new ReadOnlyMemory<float>([0.1f]);

        var point = QdrantPointMapper.ToPointStruct(chunk, embedding);

        Assert.True(point.Payload.ContainsKey("created_at"));
        Assert.False(string.IsNullOrEmpty(point.Payload["created_at"].StringValue));
    }

    [Fact]
    public void ToPointStruct_StoresTagsAsJsonInPayload()
    {
        var tags = new List<DomainTag>
        {
            new("area", "auth"),
            new("priority", "high")
        };
        var chunk = CreateChunk(tags: tags);
        var embedding = new ReadOnlyMemory<float>([0.1f]);

        var point = QdrantPointMapper.ToPointStruct(chunk, embedding);

        var tagsPayload = point.Payload["tags"].ListValue;
        Assert.Equal(2, tagsPayload.Values.Count);
        Assert.Equal("area:auth", tagsPayload.Values[0].StringValue);
        Assert.Equal("priority:high", tagsPayload.Values[1].StringValue);
    }

    [Fact]
    public void ToScoredSemanticChunk_MapsScoreAndChunkFromPayload()
    {
        var chunkId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var chunk = CreateChunk(id: chunkId);
        var embedding = new ReadOnlyMemory<float>([0.1f]);

        // Create a point, then convert back to scored chunk
        var point = QdrantPointMapper.ToPointStruct(chunk, embedding);
        var result = QdrantPointMapper.ToScoredSemanticChunk(point.Id, point.Payload, 0.95f);

        Assert.Equal(0.95, result.Score, precision: 2);
        Assert.Equal(chunkId, result.Chunk.Id);
        Assert.Equal(chunk.CanonicalSourceId, result.Chunk.CanonicalSourceId);
        Assert.Equal(chunk.Content, result.Chunk.Content);
    }

    [Fact]
    public void ToScoredSemanticChunk_PreservesTagsRoundtrip()
    {
        var tags = new List<DomainTag>
        {
            new("area", "auth"),
            new("priority", "high")
        };
        var chunk = CreateChunk(tags: tags);
        var embedding = new ReadOnlyMemory<float>([0.1f]);

        var point = QdrantPointMapper.ToPointStruct(chunk, embedding);
        var result = QdrantPointMapper.ToScoredSemanticChunk(point.Id, point.Payload, 0.8f);

        Assert.Equal(2, result.Chunk.Tags.Count);
        Assert.Equal("area", result.Chunk.Tags[0].Key);
        Assert.Equal("auth", result.Chunk.Tags[0].Value);
        Assert.Equal("priority", result.Chunk.Tags[1].Key);
        Assert.Equal("high", result.Chunk.Tags[1].Value);
    }

    [Fact]
    public void ToScoredSemanticChunk_ParsesCollectionType()
    {
        var chunk = CreateChunk(collection: SemanticCollectionType.ActivityMemory);
        var embedding = new ReadOnlyMemory<float>([0.1f]);

        var point = QdrantPointMapper.ToPointStruct(chunk, embedding);
        var result = QdrantPointMapper.ToScoredSemanticChunk(point.Id, point.Payload, 0.5f);

        Assert.Equal(SemanticCollectionType.ActivityMemory, result.Chunk.Collection);
    }
}
