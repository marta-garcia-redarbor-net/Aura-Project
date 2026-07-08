using Aura.Application.Mapping;
using Aura.Domain.WorkItems;

namespace Aura.UnitTests.Application.Mapping;

public class PullRequestMapperTests
{
    // ============================================================
    // Id parsing from ExternalId
    // ============================================================

    [Fact]
    public void ToDto_ParsesIdFromExternalId()
    {
        var workItem = CreateWorkItem("pr-142", metadata: new Dictionary<string, string>
        {
            ["pr.status"] = "passing",
            ["pr.author"] = "Alice",
            ["pr.repo"] = "Aura"
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(142, dto.Id);
    }

    [Fact]
    public void ToDto_ParsesLargeIdFromExternalId()
    {
        var workItem = CreateWorkItem("pr-9999", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(9999, dto.Id);
    }

    // ============================================================
    // Real metadata key extraction
    // ============================================================

    [Fact]
    public void ToDto_ExtractsStatusFromRealKey()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>
        {
            ["pr.status"] = "passing"
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal("passing", dto.Status);
    }

    [Fact]
    public void ToDto_ExtractsAuthorFromRealKey()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>
        {
            ["pr.author"] = "Carlos Ruiz"
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal("Carlos Ruiz", dto.Author);
    }

    [Fact]
    public void ToDto_ExtractsRepoNameFromRealKey()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>
        {
            ["pr.repo"] = "Aura.Payments"
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal("Aura.Payments", dto.RepoName);
    }

    [Fact]
    public void ToDto_ExtractsReviewerCountFromRealKey()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>
        {
            ["pr.reviewerCount"] = "3"
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(3, dto.ReviewerCount);
    }

    [Fact]
    public void ToDto_ExtractsCommentCountFromRealKey()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>
        {
            ["pr.commentCount"] = "7"
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(7, dto.CommentCount);
    }

    [Fact]
    public void ToDto_ExtractsFileCountFromRealKey()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>
        {
            ["pr.fileCount"] = "12"
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(12, dto.FileCount);
    }

    [Fact]
    public void ToDto_ExtractsIsDraftFromRealKey()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>
        {
            ["pr.isDraft"] = "True"
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.True(dto.IsDraft);
    }

    [Fact]
    public void ToDto_ExtractsSourceLinkFromRealKey()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>
        {
            ["pr.sourceLink"] = "https://dev.azure.com/org/repo/_git/repo/pullrequest/42"
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal("https://dev.azure.com/org/repo/_git/repo/pullrequest/42", dto.SourceLink);
    }

    [Fact]
    public void ToDto_ExtractsUpdatedAtFromRealKey()
    {
        var expectedDate = new DateTimeOffset(2026, 7, 5, 14, 30, 0, TimeSpan.Zero);
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>
        {
            ["pr.updatedAt"] = expectedDate.ToString("o")
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(expectedDate, dto.UpdatedAt);
    }

    // ============================================================
    // Title and Priority from WorkItem
    // ============================================================

    [Fact]
    public void ToDto_MapsTitleFromWorkItem()
    {
        var workItem = CreateWorkItem("pr-1", title: "Fix: SSO redirect", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal("Fix: SSO redirect", dto.Title);
    }

    [Fact]
    public void ToDto_MapsPriorityFromWorkItem()
    {
        var workItem = CreateWorkItem("pr-1", priority: WorkItemPriority.Critical, metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal("Critical", dto.Priority);
    }

    [Fact]
    public void ToDto_MapsCreatedAtFromCapturedAtUtc()
    {
        var capturedAt = new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>(), capturedAtUtc: capturedAt);

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(capturedAt, dto.CreatedAt);
    }

    [Fact]
    public void ToDto_MapsPriorityScoreFromWorkItem()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>(), priorityScore: 85);

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(85, dto.PriorityScore);
    }

    // ============================================================
    // Missing keys → safe defaults
    // ============================================================

    [Fact]
    public void ToDto_MissingStatusKey_DefaultsToPending()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal("pending", dto.Status);
    }

    [Fact]
    public void ToDto_MissingAuthorKey_DefaultsToEmpty()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal("", dto.Author);
    }

    [Fact]
    public void ToDto_MissingRepoKey_DefaultsToEmpty()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal("", dto.RepoName);
    }

    [Fact]
    public void ToDto_MissingReviewerCountKey_DefaultsToZero()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(0, dto.ReviewerCount);
    }

    [Fact]
    public void ToDto_MissingCommentCountKey_DefaultsToZero()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(0, dto.CommentCount);
    }

    [Fact]
    public void ToDto_MissingFileCountKey_DefaultsToZero()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(0, dto.FileCount);
    }

    [Fact]
    public void ToDto_MissingIsDraftKey_DefaultsToFalse()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.False(dto.IsDraft);
    }

    [Fact]
    public void ToDto_MissingSourceLinkKey_DefaultsToEmpty()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal("", dto.SourceLink);
    }

    [Fact]
    public void ToDto_MissingUpdatedAtKey_DefaultsToCapturedAtUtc()
    {
        var capturedAt = new DateTimeOffset(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>(), capturedAtUtc: capturedAt);

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(capturedAt, dto.UpdatedAt);
    }

    // ============================================================
    // Fields with no real data → safe defaults
    // ============================================================

    [Fact]
    public void ToDto_BranchNameAndSourceBranchName_DefaultToEmpty()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal("", dto.BranchName);
        Assert.Equal("", dto.SourceBranchName);
    }

    [Fact]
    public void ToDto_BuildStatus_DefaultsToPending()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal("pending", dto.BuildStatus);
    }

    [Fact]
    public void ToDto_ReviewApprovalsRequiredChangesRequested_DefaultToZero()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>());

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(0, dto.ReviewApprovals);
        Assert.Equal(0, dto.ReviewRequired);
        Assert.Equal(0, dto.ReviewChangesRequested);
    }

    // ============================================================
    // Invalid numeric → 0
    // ============================================================

    [Fact]
    public void ToDto_InvalidReviewerCount_DefaultsToZero()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>
        {
            ["pr.reviewerCount"] = "invalid"
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(0, dto.ReviewerCount);
    }

    [Fact]
    public void ToDto_InvalidCommentCount_DefaultsToZero()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>
        {
            ["pr.commentCount"] = "not-a-number"
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(0, dto.CommentCount);
    }

    [Fact]
    public void ToDto_InvalidFileCount_DefaultsToZero()
    {
        var workItem = CreateWorkItem("pr-1", metadata: new Dictionary<string, string>
        {
            ["pr.fileCount"] = "abc"
        });

        var dto = PullRequestMapper.ToDto(workItem);

        Assert.Equal(0, dto.FileCount);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private static WorkItem CreateWorkItem(
        string externalId,
        string title = "Test PR",
        WorkItemPriority priority = WorkItemPriority.Medium,
        Dictionary<string, string>? metadata = null,
        DateTimeOffset? capturedAtUtc = null,
        int? priorityScore = null)
    {
        return new WorkItem(
            externalId: externalId,
            title: title,
            source: "pr",
            sourceType: WorkItemSourceType.PrReview,
            priority: priority,
            metadata: metadata ?? new Dictionary<string, string>(),
            capturedAtUtc: capturedAtUtc,
            priorityScore: priorityScore);
    }
}
