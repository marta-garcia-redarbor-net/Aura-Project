namespace Aura.UnitTests.SemanticIndex;

using Aura.Application.Models;
using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;

public class SemanticQueryTests
{
    [Fact]
    public void Constructor_DefaultTopK_IsTen()
    {
        var query = new SemanticQuery { Text = "authentication patterns" };
        Assert.Equal(10, query.TopK);
    }

    [Fact]
    public void Constructor_DefaultCollection_IsNull()
    {
        var query = new SemanticQuery { Text = "test" };
        Assert.Null(query.Collection);
    }

    [Fact]
    public void Constructor_DefaultTagFilters_IsEmpty()
    {
        var query = new SemanticQuery { Text = "test" };
        Assert.Empty(query.TagFilters);
    }

    [Fact]
    public void Constructor_WithAllProperties_SetsCorrectly()
    {
        var tags = new List<DomainTag> { new("area", "security") };
        var query = new SemanticQuery
        {
            Text = "JWT validation",
            Collection = SemanticCollectionType.ProjectKnowledge,
            TagFilters = tags,
            TopK = 5
        };

        Assert.Equal("JWT validation", query.Text);
        Assert.Equal(SemanticCollectionType.ProjectKnowledge, query.Collection);
        Assert.Single(query.TagFilters);
        Assert.Equal(5, query.TopK);
    }

    [Fact]
    public void Constructor_WithActivityMemoryCollection_FiltersCorrectly()
    {
        var query = new SemanticQuery
        {
            Text = "PR review context",
            Collection = SemanticCollectionType.ActivityMemory,
            TopK = 3
        };

        Assert.Equal(SemanticCollectionType.ActivityMemory, query.Collection);
        Assert.Equal(3, query.TopK);
    }
}

public class ScoredSemanticChunkTests
{
    [Fact]
    public void Constructor_SetsChunkAndScore()
    {
        var chunk = new SemanticChunk(
            Guid.NewGuid(), "src-1", "content",
            SemanticCollectionType.ProjectKnowledge,
            new List<DomainTag>(), DateTimeOffset.UtcNow);

        var scored = new ScoredSemanticChunk { Chunk = chunk, Score = 0.95 };

        Assert.Equal(chunk, scored.Chunk);
        Assert.Equal(0.95, scored.Score);
    }

    [Fact]
    public void Score_CanBeZero()
    {
        var chunk = new SemanticChunk(
            Guid.NewGuid(), "src-2", "low relevance",
            SemanticCollectionType.ActivityMemory,
            new List<DomainTag>(), DateTimeOffset.UtcNow);

        var scored = new ScoredSemanticChunk { Chunk = chunk, Score = 0.0 };

        Assert.Equal(0.0, scored.Score);
    }

    [Fact]
    public void Score_CanBeOne()
    {
        var chunk = new SemanticChunk(
            Guid.NewGuid(), "src-3", "exact match",
            SemanticCollectionType.ProjectKnowledge,
            new List<DomainTag> { new("type", "decision") },
            DateTimeOffset.UtcNow);

        var scored = new ScoredSemanticChunk { Chunk = chunk, Score = 1.0 };

        Assert.Equal(1.0, scored.Score);
        Assert.Single(scored.Chunk.Tags);
    }
}
