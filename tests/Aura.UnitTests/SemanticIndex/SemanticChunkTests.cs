namespace Aura.UnitTests.SemanticIndex;

using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;

public class SemanticChunkTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var tags = new List<DomainTag> { new("area", "backend") };
        var createdAt = DateTimeOffset.UtcNow;

        var chunk = new SemanticChunk(
            id, "canonical-123", "Some content",
            SemanticCollectionType.ProjectKnowledge, tags, createdAt);

        Assert.Equal(id, chunk.Id);
        Assert.Equal("canonical-123", chunk.CanonicalSourceId);
        Assert.Equal("Some content", chunk.Content);
        Assert.Equal(SemanticCollectionType.ProjectKnowledge, chunk.Collection);
        Assert.Single(chunk.Tags);
        Assert.Equal(createdAt, chunk.CreatedAt);
    }

    [Fact]
    public void Constructor_EmptyCanonicalSourceId_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new SemanticChunk(Guid.NewGuid(), "", "content",
                SemanticCollectionType.ProjectKnowledge,
                new List<DomainTag>(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_EmptyContent_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new SemanticChunk(Guid.NewGuid(), "canonical-1", "",
                SemanticCollectionType.ActivityMemory,
                new List<DomainTag>(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            new SemanticChunk(Guid.Empty, "canonical-1", "content",
                SemanticCollectionType.ProjectKnowledge,
                new List<DomainTag>(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Equality_SameId_AreEqual()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var tags = new List<DomainTag>();

        var chunk1 = new SemanticChunk(id, "src-1", "content A",
            SemanticCollectionType.ProjectKnowledge, tags, now);
        var chunk2 = new SemanticChunk(id, "src-1", "content A",
            SemanticCollectionType.ProjectKnowledge, tags, now);

        Assert.Equal(chunk1, chunk2);
    }

    [Fact]
    public void Equality_DifferentId_AreNotEqual()
    {
        var now = DateTimeOffset.UtcNow;
        var tags = new List<DomainTag>();

        var chunk1 = new SemanticChunk(Guid.NewGuid(), "src-1", "content",
            SemanticCollectionType.ProjectKnowledge, tags, now);
        var chunk2 = new SemanticChunk(Guid.NewGuid(), "src-1", "content",
            SemanticCollectionType.ProjectKnowledge, tags, now);

        Assert.NotEqual(chunk1, chunk2);
    }

    [Fact]
    public void Collection_CanBeActivityMemory()
    {
        var chunk = new SemanticChunk(
            Guid.NewGuid(), "src-2", "activity data",
            SemanticCollectionType.ActivityMemory,
            new List<DomainTag> { new("type", "pr-update") },
            DateTimeOffset.UtcNow);

        Assert.Equal(SemanticCollectionType.ActivityMemory, chunk.Collection);
    }

    [Fact]
    public void Tags_AreDeepImmutable_OriginalListMutationDoesNotAffectChunk()
    {
        var originalTags = new List<DomainTag> { new("area", "backend") };
        var chunk = new SemanticChunk(
            Guid.NewGuid(), "src-3", "content",
            SemanticCollectionType.ProjectKnowledge, originalTags, DateTimeOffset.UtcNow);

        originalTags.Add(new DomainTag("extra", "injected"));

        Assert.Single(chunk.Tags);
        Assert.Equal(new DomainTag("area", "backend"), chunk.Tags[0]);
    }

    [Fact]
    public void Tags_AreDeepImmutable_ClearingOriginalListDoesNotAffectChunk()
    {
        var originalTags = new List<DomainTag>
        {
            new("area", "backend"),
            new("type", "decision")
        };
        var chunk = new SemanticChunk(
            Guid.NewGuid(), "src-4", "content",
            SemanticCollectionType.ActivityMemory, originalTags, DateTimeOffset.UtcNow);

        originalTags.Clear();

        Assert.Equal(2, chunk.Tags.Count);
        Assert.Equal(new DomainTag("area", "backend"), chunk.Tags[0]);
        Assert.Equal(new DomainTag("type", "decision"), chunk.Tags[1]);
    }
}
