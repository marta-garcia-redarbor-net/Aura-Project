using Aura.Application.Models;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Connectors.PrReview;

namespace Aura.UnitTests.Infrastructure;

public class PrReviewWorkItemMapperIdentityTests
{
    [Fact]
    public void TryMap_WithReviewerIdentityOid_PersistsIdentityKeys()
    {
        var mapper = new PrReviewWorkItemMapper();
        var dto = new PrReviewDto
        {
            PullRequestId = 42,
            Title = "PR with identities",
            RepoName = "Aura",
            Author = "Jane",
            Reviewers = ["Jane"],
            ReviewerIdentities =
            [
                new PrReviewerIdentity("user-abc", "Jane", false),
                new PrReviewerIdentity("group-xyz", "Backend Team", true)
            ]
        };

        var result = mapper.TryMap(dto, out var workItem);

        Assert.True(result);
        Assert.NotNull(workItem);
        Assert.Equal("user-abc", workItem!.Metadata[PrMetadataKeys.ReviewerOid(0)]);
        Assert.Equal("Jane", workItem.Metadata[PrMetadataKeys.ReviewerDisplayName(0)]);
        Assert.Equal("False", workItem.Metadata[PrMetadataKeys.ReviewerIsContainer(0)]);
        Assert.Equal("group-xyz", workItem.Metadata[PrMetadataKeys.ReviewerOid(1)]);
        Assert.Equal("Backend Team", workItem.Metadata[PrMetadataKeys.ReviewerDisplayName(1)]);
        Assert.Equal("True", workItem.Metadata[PrMetadataKeys.ReviewerIsContainer(1)]);
    }

    [Fact]
    public void TryMap_WithNullReviewerOid_OmitsOidKeyButPersistsDisplayName()
    {
        var mapper = new PrReviewWorkItemMapper();
        var dto = new PrReviewDto
        {
            PullRequestId = 43,
            Title = "PR without reviewer oid",
            RepoName = "Aura",
            Author = "Jane",
            Reviewers = ["Jane"],
            ReviewerIdentities =
            [
                new PrReviewerIdentity(null, "Jane", false)
            ]
        };

        var result = mapper.TryMap(dto, out var workItem);

        Assert.True(result);
        Assert.NotNull(workItem);
        Assert.False(workItem!.Metadata.ContainsKey(PrMetadataKeys.ReviewerOid(0)));
        Assert.Equal("Jane", workItem.Metadata[PrMetadataKeys.ReviewerDisplayName(0)]);
        Assert.Equal("False", workItem.Metadata[PrMetadataKeys.ReviewerIsContainer(0)]);
    }

    [Fact]
    public void TryMap_WithReviewerIdentities_PreservesReviewerCount()
    {
        var mapper = new PrReviewWorkItemMapper();
        var dto = new PrReviewDto
        {
            PullRequestId = 44,
            Title = "PR count check",
            RepoName = "Aura",
            Author = "Jane",
            Reviewers = ["Jane", "Backend Team"],
            ReviewerIdentities =
            [
                new PrReviewerIdentity("user-abc", "Jane", false),
                new PrReviewerIdentity("group-xyz", "Backend Team", true)
            ]
        };

        var result = mapper.TryMap(dto, out var workItem);

        Assert.True(result);
        Assert.NotNull(workItem);
        Assert.Equal("2", workItem!.Metadata[PrMetadataKeys.ReviewerCount]);
    }

    [Fact]
    public void TryMap_WithUserOid_SetsOwnerUserScopeOnWorkItemAndMetadata()
    {
        var mapper = new PrReviewWorkItemMapper();
        var dto = new PrReviewDto
        {
            PullRequestId = 45,
            Title = "Scoped PR",
            RepoName = "Aura",
            Author = "Jane",
            UserOid = "oid-real-45",
            Reviewers = ["Jane"]
        };

        var result = mapper.TryMap(dto, out var workItem);

        Assert.True(result);
        Assert.NotNull(workItem);
        Assert.Equal("oid-real-45", workItem!.OwnerUserId);
        Assert.Equal("oid-real-45", workItem.Metadata[WorkItemSignalKeys.TargetOwnerUserId]);
        Assert.Equal("Jane", workItem.Metadata[WorkItemSignalKeys.CanonicalSender]);
    }
}
