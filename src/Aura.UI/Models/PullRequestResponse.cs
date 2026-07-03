namespace Aura.UI.Models;

public sealed record PullRequestResponse(
    int Id,
    string Title,
    string RepoName,
    string Author,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Status,
    int ReviewerCount,
    int CommentCount,
    int FileCount,
    string SourceLink,
    bool IsDraft,
    string Priority,
    string BranchName,
    string SourceBranchName,
    string BuildStatus,
    int ReviewApprovals,
    int ReviewRequired,
    int ReviewChangesRequested);
