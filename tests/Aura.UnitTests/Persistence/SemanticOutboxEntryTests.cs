using Aura.Application.Models;
using Aura.Domain.SemanticIndex.Enums;

namespace Aura.UnitTests.Persistence;

public class SemanticOutboxEntryTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        var canonicalSourceId = "src-001";
        var content = "Some evidence content";
        var collection = SemanticCollectionType.ProjectKnowledge;
        var createdAt = DateTimeOffset.UtcNow;

        var entry = new SemanticOutboxEntry(id, canonicalSourceId, content, collection, createdAt);

        Assert.Equal(id, entry.Id);
        Assert.Equal(canonicalSourceId, entry.CanonicalSourceId);
        Assert.Equal(content, entry.Content);
        Assert.Equal(collection, entry.Collection);
        Assert.Equal(createdAt, entry.CreatedAt);
        Assert.False(entry.Processed);
        Assert.Null(entry.ProcessedAt);
        Assert.Null(entry.Error);
    }

    [Fact]
    public void Constructor_DefaultsProcessedToFalse()
    {
        var entry = new SemanticOutboxEntry(
            Guid.NewGuid(), "src-002", "content",
            SemanticCollectionType.ActivityMemory, DateTimeOffset.UtcNow);

        Assert.False(entry.Processed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_ThrowsOnEmptyCanonicalSourceId(string? canonicalSourceId)
    {
        Assert.Throws<ArgumentException>(() =>
            new SemanticOutboxEntry(
                Guid.NewGuid(), canonicalSourceId!, "content",
                SemanticCollectionType.ProjectKnowledge, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_ThrowsOnEmptyContent(string? content)
    {
        Assert.Throws<ArgumentException>(() =>
            new SemanticOutboxEntry(
                Guid.NewGuid(), "src-003", content!,
                SemanticCollectionType.ProjectKnowledge, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Constructor_ThrowsOnEmptyGuid()
    {
        Assert.Throws<ArgumentException>(() =>
            new SemanticOutboxEntry(
                Guid.Empty, "src-004", "content",
                SemanticCollectionType.ProjectKnowledge, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MarkProcessed_SetsProcessedAndTimestamp()
    {
        var entry = new SemanticOutboxEntry(
            Guid.NewGuid(), "src-005", "content",
            SemanticCollectionType.ProjectKnowledge, DateTimeOffset.UtcNow);

        var before = DateTimeOffset.UtcNow;
        entry.MarkProcessed();
        var after = DateTimeOffset.UtcNow;

        Assert.True(entry.Processed);
        Assert.NotNull(entry.ProcessedAt);
        Assert.InRange(entry.ProcessedAt!.Value, before, after);
        Assert.Null(entry.Error);
    }

    [Fact]
    public void MarkFailed_SetsErrorAndLeavesUnprocessed()
    {
        var entry = new SemanticOutboxEntry(
            Guid.NewGuid(), "src-006", "content",
            SemanticCollectionType.ActivityMemory, DateTimeOffset.UtcNow);

        entry.MarkFailed("Embedding service unavailable");

        Assert.False(entry.Processed);
        Assert.Null(entry.ProcessedAt);
        Assert.Equal("Embedding service unavailable", entry.Error);
    }
}
