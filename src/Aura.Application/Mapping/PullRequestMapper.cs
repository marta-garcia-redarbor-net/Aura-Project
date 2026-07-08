using Aura.Application.Models;
using Aura.Domain.WorkItems;

namespace Aura.Application.Mapping;

/// <summary>
/// Static mapper from <see cref="WorkItem"/> to <see cref="PullRequestDto"/>.
/// Reads REAL metadata keys written by PrReviewWorkItemMapper.
/// </summary>
public static class PullRequestMapper
{
    private const string PrExternalIdPrefix = "pr-";

    public static PullRequestDto ToDto(WorkItem item)
    {
        return new PullRequestDto(
            Id: ParseIdFromExternalId(item.ExternalId),
            Title: item.Title,
            RepoName: GetMetadataString(item, "pr.repo", ""),
            Author: GetMetadataString(item, "pr.author", ""),
            CreatedAt: item.CapturedAtUtc,
            UpdatedAt: GetMetadataDateTimeOffset(item, "pr.updatedAt", item.CapturedAtUtc),
            Status: GetMetadataString(item, "pr.status", "pending"),
            ReviewerCount: GetMetadataInt(item, "pr.reviewerCount"),
            CommentCount: GetMetadataInt(item, "pr.commentCount"),
            FileCount: GetMetadataInt(item, "pr.fileCount"),
            SourceLink: GetMetadataString(item, "pr.sourceLink", ""),
            IsDraft: GetMetadataBool(item, "pr.isDraft"),
            Priority: item.Priority.ToString(),
            BranchName: "",
            SourceBranchName: "",
            BuildStatus: "pending",
            ReviewApprovals: 0,
            ReviewRequired: 0,
            ReviewChangesRequested: 0,
            PriorityScore: item.PriorityScore);
    }

    private static int ParseIdFromExternalId(string externalId)
    {
        if (externalId.StartsWith(PrExternalIdPrefix, StringComparison.Ordinal)
            && externalId.Length > PrExternalIdPrefix.Length)
        {
            if (int.TryParse(externalId.AsSpan(PrExternalIdPrefix.Length), out var id))
            {
                return id;
            }
        }

        return 0;
    }

    private static string GetMetadataString(WorkItem item, string key, string defaultValue)
    {
        return item.Metadata.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value)
            ? value
            : defaultValue;
    }

    private static int GetMetadataInt(WorkItem item, string key)
    {
        return item.Metadata.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
            ? parsed
            : 0;
    }

    private static bool GetMetadataBool(WorkItem item, string key)
    {
        return item.Metadata.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed) && parsed;
    }

    private static DateTimeOffset GetMetadataDateTimeOffset(WorkItem item, string key, DateTimeOffset fallback)
    {
        return item.Metadata.TryGetValue(key, out var value) && DateTimeOffset.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }
}
