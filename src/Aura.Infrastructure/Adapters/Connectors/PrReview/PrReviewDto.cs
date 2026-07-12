namespace Aura.Infrastructure.Adapters.Connectors.PrReview;

internal sealed record PrReviewDto
{
    /// <summary>Owner oid for user-scoped persistence when available.</summary>
    public string? UserOid { get; init; }

    /// <summary>Azure DevOps pull request ID.</summary>
    public int PullRequestId { get; init; }

    /// <summary>PR title.</summary>
    public string? Title { get; init; }

    /// <summary>Repository name (e.g. "Aura").</summary>
    public string? RepoName { get; init; }

    /// <summary>Display name of the PR author.</summary>
    public string? Author { get; init; }

    /// <summary>When the PR was created.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>When the PR was last updated.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>ADO status: "active", "approved", "completed", etc.</summary>
    public string? Status { get; init; }

    /// <summary>List of reviewer display names.</summary>
    public IReadOnlyList<string>? Reviewers { get; init; }

    /// <summary>Identity-rich reviewer model used for oid/container-aware attention derivation.</summary>
    public IReadOnlyList<PrReviewerIdentity>? ReviewerIdentities { get; init; }

    /// <summary>Number of comments on the PR.</summary>
    public int CommentCount { get; init; }

    /// <summary>Number of files changed.</summary>
    public int FileCount { get; init; }

    /// <summary>Deep link URL to the PR in Azure DevOps.</summary>
    public string? SourceLink { get; init; }

    /// <summary>Whether the PR is a draft.</summary>
    public bool IsDraft { get; init; }

    /// <summary>Priority hint from the provider (critical/high/medium/low).</summary>
    public string? Priority { get; init; }
}
