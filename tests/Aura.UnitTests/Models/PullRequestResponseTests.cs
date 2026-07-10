using Aura.UI.Models;

namespace Aura.UnitTests.Models;

public class PullRequestResponseTests
{
    [Fact]
    public void AttentionScope_DefaultsToUnknown_WhenOmitted()
    {
        var response = new PullRequestResponse(
            Id: 1,
            Title: "Test PR",
            RepoName: "repo",
            Author: "Alice",
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            Status: "active",
            ReviewerCount: 1,
            CommentCount: 0,
            FileCount: 1,
            SourceLink: "https://dev.azure.com/pr/1",
            IsDraft: false,
            Priority: "high",
            BranchName: "main",
            SourceBranchName: "feature",
            BuildStatus: "passing",
            ReviewApprovals: 0,
            ReviewRequired: 1,
            ReviewChangesRequested: 0);

        Assert.Equal("unknown", response.AttentionScope);
    }

    [Fact]
    public void AttentionScope_PropagatesApiValue_WhenProvided()
    {
        var response = new PullRequestResponse(
            Id: 1,
            Title: "Test PR",
            RepoName: "repo",
            Author: "Alice",
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            Status: "active",
            ReviewerCount: 1,
            CommentCount: 0,
            FileCount: 1,
            SourceLink: "https://dev.azure.com/pr/1",
            IsDraft: false,
            Priority: "high",
            BranchName: "main",
            SourceBranchName: "feature",
            BuildStatus: "passing",
            ReviewApprovals: 0,
            ReviewRequired: 1,
            ReviewChangesRequested: 0,
            AttentionScope: "direct");

        Assert.Equal("direct", response.AttentionScope);
    }
}

public class PrPreviewItemResponseTests
{
    [Fact]
    public void AttentionScope_DefaultsToUnknown_WhenOmitted()
    {
        var response = new PrPreviewItemResponse(
            Title: "Test PR",
            PrDisplayName: "#1 Test PR",
            BranchName: "main",
            BuildStatus: "passing",
            ReviewApprovals: 1,
            ReviewRequired: 2,
            ReviewChangesRequested: 0,
            Author: "Alice",
            UpdatedAt: DateTimeOffset.UtcNow,
            RelativeTimestamp: "1h ago",
            SourceLink: "https://dev.azure.com/pr/1",
            IsDraft: false,
            Priority: "high");

        Assert.Equal("unknown", response.AttentionScope);
    }

    [Fact]
    public void AttentionScope_PropagatesValue_WhenProvided()
    {
        var response = new PrPreviewItemResponse(
            Title: "Test PR",
            PrDisplayName: "#1 Test PR",
            BranchName: "main",
            BuildStatus: "passing",
            ReviewApprovals: 1,
            ReviewRequired: 2,
            ReviewChangesRequested: 0,
            Author: "Alice",
            UpdatedAt: DateTimeOffset.UtcNow,
            RelativeTimestamp: "1h ago",
            SourceLink: "https://dev.azure.com/pr/1",
            IsDraft: false,
            Priority: "high",
            AttentionScope: "group");

        Assert.Equal("group", response.AttentionScope);
    }
}
