using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Connectors.PrReview;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aura.UnitTests.Adapters.Connectors.PrReview;

public class PrReviewConnectorAdapterTests
{
    [Fact]
    public async Task ExecuteAsync_MapsAndEnqueuesAllValidFixtures()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var fixtures = new[]
        {
            new PrReviewDto { PullRequestId = 1, Title = "PR One", RepoName = "repo1", Author = "Alice", Status = "active", Priority = "high" },
            new PrReviewDto { PullRequestId = 2, Title = "PR Two", RepoName = "repo2", Author = "Bob", Status = "active", Priority = "low" }
        };
        var adapter = new PrReviewConnectorAdapter(
            NullLogger<PrReviewConnectorAdapter>.Instance, buffer, new PrReviewWorkItemMapper(), () => fixtures);
        var request = CreateRequest();

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        buffer.Received(2).Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>());
        buffer.Received(2).Enqueue(Arg.Is<Aura.Domain.WorkItems.WorkItem>(item =>
            item.SourceType == WorkItemSourceType.PrReview));
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Success, result.Status);
        Assert.Equal(request.WindowEnd, result.MaxProcessedAt);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsInvalidFixtures_WithPartialFailure()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var fixtures = new[]
        {
            new PrReviewDto { PullRequestId = 1, Title = "Valid PR", RepoName = "repo", Author = "Alice", Status = "active", Priority = "high" },
            new PrReviewDto { PullRequestId = 0, Title = "Invalid ID", RepoName = "repo", Author = "Bob", Status = "active" },
            new PrReviewDto { PullRequestId = 3, Title = "Valid PR 2", RepoName = "repo", Author = "Charlie", Status = "active", Priority = "medium" }
        };
        var adapter = new PrReviewConnectorAdapter(
            NullLogger<PrReviewConnectorAdapter>.Instance, buffer, new PrReviewWorkItemMapper(), () => fixtures);
        var request = CreateRequest();

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        buffer.Received(2).Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>());
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.PartialFailure, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutFixtureProvider_UsesDefaultFixtures()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var adapter = new PrReviewConnectorAdapter(
            NullLogger<PrReviewConnectorAdapter>.Instance, buffer, new PrReviewWorkItemMapper());
        var request = CreateRequest();

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        buffer.Received(6).Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>());
        Assert.Equal(6, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Success, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceProvider_UsesProviderInsteadOfFixtures()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var provider = Substitute.For<IMessageSourceProvider<PrReviewDto>>();
        var providerPayloads = new[]
        {
            new PrReviewDto
            {
                PullRequestId = 100, Title = "ADO PR from provider", RepoName = "Aura",
                Author = "John Doe", Status = "active", Priority = "high"
            }
        };
        var request = CreateRequest();
        provider.FetchAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PrReviewDto>>(providerPayloads));

        var adapter = new PrReviewConnectorAdapter(
            NullLogger<PrReviewConnectorAdapter>.Instance, buffer, new PrReviewWorkItemMapper(),
            fixtureProvider: () => throw new InvalidOperationException("Fixture must not be called when provider exists"),
            sourceProvider: provider);

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        await provider.Received(1).FetchAsync(request, Arg.Any<CancellationToken>());
        buffer.Received(1).Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>());
        Assert.Equal(1, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Success, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_SourceProvider_HttpError_ReturnsFailure()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var provider = Substitute.For<IMessageSourceProvider<PrReviewDto>>();
        var request = CreateRequest();
        provider.FetchAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<PrReviewDto>>(
                new HttpRequestException("Connection refused")));

        var adapter = new PrReviewConnectorAdapter(
            NullLogger<PrReviewConnectorAdapter>.Instance, buffer, new PrReviewWorkItemMapper(),
            sourceProvider: provider);

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        Assert.Equal(0, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Failure, result.Status);
        Assert.Contains("Azure DevOps HTTP error", result.FailureReason);
    }

    [Fact]
    public void ConnectorName_ReturnsPr()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var adapter = new PrReviewConnectorAdapter(
            NullLogger<PrReviewConnectorAdapter>.Instance, buffer, new PrReviewWorkItemMapper());

        Assert.Equal("pr", adapter.ConnectorName);
    }

    [Fact]
    public void LoadDefaultFixtures_ReturnsSixItems()
    {
        var fixtures = PrReviewConnectorAdapter.LoadDefaultFixtures();

        Assert.Equal(6, fixtures.Count);
    }

    [Fact]
    public async Task Mapper_SetsCorrectSourceTypeAndMetadata()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var capturedItems = new List<Aura.Domain.WorkItems.WorkItem>();
        buffer.When(b => b.Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>()))
            .Do(ci => capturedItems.Add(ci.Arg<Aura.Domain.WorkItems.WorkItem>()));

        var fixtures = new[]
        {
            new PrReviewDto
            {
                PullRequestId = 42,
                Title = "Test PR",
                RepoName = "Aura",
                Author = "Carlos Ruiz",
                CreatedAt = new DateTimeOffset(2026, 07, 1, 10, 0, 0, TimeSpan.Zero),
                Status = "active",
                Reviewers = ["Ana López"],
                CommentCount = 5,
                FileCount = 3,
                SourceLink = "https://dev.azure.com/auraorg/Aura/_git/Aura/pullrequest/42",
                IsDraft = false,
                Priority = "high"
            }
        };
        var adapter = new PrReviewConnectorAdapter(
            NullLogger<PrReviewConnectorAdapter>.Instance, buffer, new PrReviewWorkItemMapper(), () => fixtures);

        await adapter.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.Single(capturedItems);
        var item = capturedItems[0];
        Assert.Equal(WorkItemSourceType.PrReview, item.SourceType);
        Assert.Equal("pr", item.Source);
        Assert.Equal("pr-42", item.ExternalId);
        Assert.Equal("Test PR", item.Title);
        Assert.Equal("active", item.Metadata["pr.status"]);
        Assert.Equal("Aura", item.Metadata["pr.repo"]);
        Assert.Equal("Carlos Ruiz", item.Metadata["pr.author"]);
        Assert.Equal("Ana López", item.Metadata["pr.reviewers"]);
        Assert.Equal("1", item.Metadata["pr.reviewerCount"]);
        Assert.Equal("5", item.Metadata["pr.commentCount"]);
        Assert.Equal("3", item.Metadata["pr.fileCount"]);
        Assert.Equal("False", item.Metadata["pr.isDraft"]);
        Assert.Equal(WorkItemPriority.High, item.Priority);
    }

    [Fact]
    public async Task Mapper_DraftPr_DefaultsToLowPriority()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var capturedItems = new List<Aura.Domain.WorkItems.WorkItem>();
        buffer.When(b => b.Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>()))
            .Do(ci => capturedItems.Add(ci.Arg<Aura.Domain.WorkItems.WorkItem>()));

        var fixtures = new[]
        {
            new PrReviewDto
            {
                PullRequestId = 99,
                Title = "Draft PR",
                RepoName = "Aura",
                Author = "Dev",
                Status = "active",
                IsDraft = true,
                Priority = null
            }
        };
        var adapter = new PrReviewConnectorAdapter(
            NullLogger<PrReviewConnectorAdapter>.Instance, buffer, new PrReviewWorkItemMapper(), () => fixtures);

        await adapter.ExecuteAsync(CreateRequest(), CancellationToken.None);

        Assert.Single(capturedItems);
        var item = capturedItems[0];
        Assert.Equal(WorkItemPriority.Low, item.Priority);
        Assert.Equal("defaulted-low-draft", item.Metadata["pr.priority.resolution"]);
    }

    private static ConnectorExecutionRequest CreateRequest() =>
        new(
            new CheckpointIdentity("pr", "azdo", "acme"),
            new DateTimeOffset(2026, 07, 1, 00, 00, 00, TimeSpan.Zero),
            new DateTimeOffset(2026, 07, 1, 23, 59, 59, TimeSpan.Zero));
}
