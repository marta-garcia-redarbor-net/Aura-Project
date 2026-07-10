using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Connectors.PrReview;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aura.UnitTests.Adapters.Connectors.PrReview;

public class PrReviewConnectorAdapterAttentionScopeTests
{
    [Fact]
    public async Task ExecuteAsync_FixturePath_SetsAttentionScopeDirect_OnAllWorkItems()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var capturedItems = new List<WorkItem>();
        buffer.When(b => b.Enqueue(Arg.Any<WorkItem>()))
            .Do(ci => capturedItems.Add(ci.Arg<WorkItem>()));

        var fixtures = new[]
        {
            new PrReviewDto
            {
                PullRequestId = 1, Title = "Fixture PR 1", RepoName = "repo",
                Author = "Alice", Status = "active", Priority = "high"
            },
            new PrReviewDto
            {
                PullRequestId = 2, Title = "Fixture PR 2", RepoName = "repo",
                Author = "Bob", Status = "active", Priority = "medium"
            }
        };

        // null sourceProvider = fixture path (demo mode)
        var adapter = new PrReviewConnectorAdapter(
            NullLogger<PrReviewConnectorAdapter>.Instance, buffer,
            new PrReviewWorkItemMapper(), () => fixtures, sourceProvider: null);

        await adapter.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(2, capturedItems.Count);
        foreach (var item in capturedItems)
        {
            Assert.Equal("direct", item.Metadata[PrMetadataKeys.AttentionScope]);
        }
    }

    [Fact]
    public async Task ExecuteAsync_RealProviderPath_DoesNotOverrideAttentionScope()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var capturedItems = new List<WorkItem>();
        buffer.When(b => b.Enqueue(Arg.Any<WorkItem>()))
            .Do(ci => capturedItems.Add(ci.Arg<WorkItem>()));

        var provider = Substitute.For<IMessageSourceProvider<PrReviewDto>>();
        var realPayloads = new[]
        {
            new PrReviewDto
            {
                PullRequestId = 100, Title = "Real PR", RepoName = "Aura",
                Author = "John", Status = "active", Priority = "high",
                ReviewerIdentities =
                [
                    new PrReviewerIdentity("reviewer-oid", "Reviewer", false)
                ]
            }
        };
        provider.FetchAsync(Arg.Any<ConnectorExecutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PrReviewDto>>(realPayloads));

        // sourceProvider present = real data path
        var adapter = new PrReviewConnectorAdapter(
            NullLogger<PrReviewConnectorAdapter>.Instance, buffer,
            new PrReviewWorkItemMapper(),
            fixtureProvider: () => throw new InvalidOperationException("Should not be called"),
            sourceProvider: provider);

        await adapter.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.Single(capturedItems);
        // Real path: mapper does NOT set pr.attentionScope in BuildMetadata
        // (that's computed later by PullRequestMapper.DeriveAttentionScope)
        // So the key should NOT be present — proving we didn't override it
        Assert.False(capturedItems[0].Metadata.ContainsKey(PrMetadataKeys.AttentionScope));
    }

    private static ConnectorExecutionRequest CreateRequest() =>
        new(
            new CheckpointIdentity("pr", "azdo", "acme"),
            new DateTimeOffset(2026, 07, 1, 00, 00, 00, TimeSpan.Zero),
            new DateTimeOffset(2026, 07, 1, 23, 59, 59, TimeSpan.Zero));
}
