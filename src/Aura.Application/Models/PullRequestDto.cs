namespace Aura.Application.Models;

/// <summary>
/// DTO specific to Pull Requests, mapped from WorkItem (SourceType=PrReview).
/// Field names align with <c>PullRequestResponse</c> in the UI layer.
/// </summary>
public sealed record PullRequestDto(
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
    int ReviewChangesRequested,
    int? PriorityScore);
