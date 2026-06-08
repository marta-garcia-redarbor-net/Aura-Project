using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;
using Aura.Infrastructure.Adapters.SemanticIndex;

namespace Aura.UnitTests.VectorStore;

public class QdrantSemanticContextAdapterTests
{
    private static SemanticChunk CreateChunk(
        SemanticCollectionType collection = SemanticCollectionType.ProjectKnowledge,
        IReadOnlyList<DomainTag>? tags = null)
    {
        return new SemanticChunk(
            Guid.NewGuid(),
            "src-1",
            "Test content",
            collection,
            tags ?? [],
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public void MatchesTags_NoFilters_ReturnsTrue()
    {
        var chunk = CreateChunk(tags: [new DomainTag("area", "auth")]);

        var result = QdrantSemanticContextAdapter.MatchesTags(chunk, []);

        Assert.True(result);
    }

    [Fact]
    public void MatchesTags_MatchingFilter_ReturnsTrue()
    {
        var chunk = CreateChunk(tags: [new DomainTag("area", "auth"), new DomainTag("priority", "high")]);
        var filters = new List<DomainTag> { new("area", "auth") };

        var result = QdrantSemanticContextAdapter.MatchesTags(chunk, filters);

        Assert.True(result);
    }

    [Fact]
    public void MatchesTags_NonMatchingFilter_ReturnsFalse()
    {
        var chunk = CreateChunk(tags: [new DomainTag("area", "auth")]);
        var filters = new List<DomainTag> { new("area", "billing") };

        var result = QdrantSemanticContextAdapter.MatchesTags(chunk, filters);

        Assert.False(result);
    }

    [Fact]
    public void MatchesTags_MultipleFiltersPartialMatch_ReturnsFalse()
    {
        var chunk = CreateChunk(tags: [new DomainTag("area", "auth")]);
        var filters = new List<DomainTag> { new("area", "auth"), new("priority", "high") };

        var result = QdrantSemanticContextAdapter.MatchesTags(chunk, filters);

        Assert.False(result);
    }

    [Fact]
    public void MatchesTags_MultipleFiltersAllMatch_ReturnsTrue()
    {
        var chunk = CreateChunk(tags: [new DomainTag("area", "auth"), new DomainTag("priority", "high")]);
        var filters = new List<DomainTag> { new("area", "auth"), new("priority", "high") };

        var result = QdrantSemanticContextAdapter.MatchesTags(chunk, filters);

        Assert.True(result);
    }

    [Fact]
    public void MatchesTags_ChunkWithNoTags_FilterPresent_ReturnsFalse()
    {
        var chunk = CreateChunk(tags: []);
        var filters = new List<DomainTag> { new("area", "auth") };

        var result = QdrantSemanticContextAdapter.MatchesTags(chunk, filters);

        Assert.False(result);
    }
}
