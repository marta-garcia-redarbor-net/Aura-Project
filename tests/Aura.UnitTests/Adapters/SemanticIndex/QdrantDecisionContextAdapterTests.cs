using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aura.UnitTests.Adapters.SemanticIndex;

public class QdrantDecisionContextAdapterTests
{
    private sealed class StubSemanticRetriever(Func<SemanticQuery, CancellationToken, Task<IReadOnlyList<ScoredSemanticChunk>>> retrieve)
        : ISemanticContextRetriever
    {
        public Task<IReadOnlyList<ScoredSemanticChunk>> RetrieveAsync(SemanticQuery query, CancellationToken ct)
            => retrieve(query, ct);
    }

    private static WorkItem CreateItem(string title = "Production incident in payments")
        => new(
            externalId: "teams-seed-001",
            title: title,
            source: "messages",
            sourceType: WorkItemSourceType.TeamsMessage,
            priority: WorkItemPriority.Critical,
            metadata: new Dictionary<string, string>());

    [Fact]
    public async Task RetrieveAsync_WhenSemanticRetrievalSucceeds_MapsDecisionContextItems()
    {
        var chunk = new SemanticChunk(
            id: Guid.NewGuid(),
            canonicalSourceId: "outlook-seed-001",
            content: "Customer reports payment failures in production.",
            collection: SemanticCollectionType.ActivityMemory,
            tags: [],
            createdAt: DateTimeOffset.UtcNow);

        var retriever = new StubSemanticRetriever((_, _) => Task.FromResult<IReadOnlyList<ScoredSemanticChunk>>([
            new ScoredSemanticChunk { Chunk = chunk, Score = 0.93 }
        ]));

        var sut = new QdrantDecisionContextAdapter(retriever, NullLogger<QdrantDecisionContextAdapter>.Instance);

        var result = await sut.RetrieveAsync(CreateItem(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("outlook-seed-001", result[0].CanonicalSourceId);
        Assert.Equal("Customer reports payment failures in production.", result[0].ContentSnippet);
        Assert.Equal("ActivityMemory", result[0].SourceType);
        Assert.Equal(0.93, result[0].RelevanceScore, 3);
    }

    [Fact]
    public async Task RetrieveAsync_WhenSemanticRetrievalTimesOut_ReturnsEmptyList()
    {
        var retriever = new StubSemanticRetriever(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return [];
        });

        var sut = new QdrantDecisionContextAdapter(retriever, NullLogger<QdrantDecisionContextAdapter>.Instance);

        var result = await sut.RetrieveAsync(CreateItem(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task RetrieveAsync_WhenSemanticRetrieverThrows_ReturnsEmptyList()
    {
        var retriever = new StubSemanticRetriever((_, _) => throw new InvalidOperationException("qdrant down"));
        var sut = new QdrantDecisionContextAdapter(retriever, NullLogger<QdrantDecisionContextAdapter>.Instance);

        var result = await sut.RetrieveAsync(CreateItem(), CancellationToken.None);

        Assert.Empty(result);
    }
}
