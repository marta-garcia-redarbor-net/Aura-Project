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
    private const string AttentionScopeDirect = "direct";
    private const string AttentionScopeGroup = "group";
    private const string AttentionScopeBoth = "both";
    private const string AttentionScopeNone = "none";
    private const string AttentionScopeUnknown = "unknown";

    public static PullRequestDto ToDto(
        WorkItem item,
        string? currentUserOid = null,
        string? currentUserDisplayName = null)
    {
        var attentionScope = DeriveAttentionScope(item, currentUserOid, currentUserDisplayName, out var fallback);
        TryPersistDerivedAttentionMetadata(item, attentionScope, fallback);

        return new PullRequestDto(
            Id: ParseIdFromExternalId(item.ExternalId),
            Title: item.Title,
            RepoName: GetMetadataString(item, PrMetadataKeys.Repo, ""),
            Author: GetMetadataString(item, PrMetadataKeys.Author, ""),
            CreatedAt: item.CapturedAtUtc,
            UpdatedAt: GetMetadataDateTimeOffset(item, PrMetadataKeys.UpdatedAt, item.CapturedAtUtc),
            Status: GetMetadataString(item, PrMetadataKeys.Status, "pending"),
            ReviewerCount: GetMetadataInt(item, PrMetadataKeys.ReviewerCount),
            CommentCount: GetMetadataInt(item, PrMetadataKeys.CommentCount),
            FileCount: GetMetadataInt(item, PrMetadataKeys.FileCount),
            SourceLink: GetMetadataString(item, PrMetadataKeys.SourceLink, ""),
            IsDraft: GetMetadataBool(item, PrMetadataKeys.IsDraft),
            Priority: item.Priority.ToString(),
            BranchName: GetMetadataString(item, PrMetadataKeys.Branch, ""),
            SourceBranchName: GetMetadataString(item, PrMetadataKeys.SourceBranch, ""),
            BuildStatus: "pending",
            ReviewApprovals: 0,
            ReviewRequired: 0,
            ReviewChangesRequested: 0,
            PriorityScore: item.PriorityScore,
            AttentionScope: attentionScope);
    }

    private static string DeriveAttentionScope(
        WorkItem item,
        string? currentUserOid,
        string? currentUserDisplayName,
        out string? fallback)
    {
        fallback = null;

        var precomputedScope = GetMetadataString(item, PrMetadataKeys.AttentionScope, "");
        if (!string.IsNullOrWhiteSpace(precomputedScope))
        {
            return precomputedScope;
        }

        var reviewerCount = GetMetadataInt(item, PrMetadataKeys.ReviewerCount);

        if (!string.IsNullOrWhiteSpace(currentUserOid))
        {
            var hasDirectOidMatch = false;
            var hasGroupOidMatch = false;

            for (var i = 0; i < reviewerCount; i++)
            {
                var oid = GetMetadataString(item, PrMetadataKeys.ReviewerOid(i), "");
                if (!string.Equals(oid, currentUserOid, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var isContainer = GetMetadataBool(item, PrMetadataKeys.ReviewerIsContainer(i));
                if (isContainer)
                {
                    hasGroupOidMatch = true;
                }
                else
                {
                    hasDirectOidMatch = true;
                }
            }

            if (hasDirectOidMatch && hasGroupOidMatch)
            {
                return AttentionScopeBoth;
            }

            if (hasDirectOidMatch)
            {
                return AttentionScopeDirect;
            }

            if (hasGroupOidMatch)
            {
                return AttentionScopeGroup;
            }
        }

        if (!string.IsNullOrWhiteSpace(currentUserDisplayName))
        {
            for (var i = 0; i < reviewerCount; i++)
            {
                var oid = GetMetadataString(item, PrMetadataKeys.ReviewerOid(i), "");
                if (!string.IsNullOrWhiteSpace(oid))
                {
                    continue;
                }

                var displayName = GetMetadataString(item, PrMetadataKeys.ReviewerDisplayName(i), "");
                if (string.Equals(displayName, currentUserDisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    fallback = "displayName";
                    return AttentionScopeDirect;
                }
            }

            return string.IsNullOrWhiteSpace(currentUserOid)
                ? AttentionScopeUnknown
                : AttentionScopeNone;
        }

        return string.IsNullOrWhiteSpace(currentUserOid)
            ? AttentionScopeUnknown
            : AttentionScopeNone;
    }

    private static void TryPersistDerivedAttentionMetadata(WorkItem item, string attentionScope, string? fallback)
    {
        if (item.Metadata is not IDictionary<string, string> mutableMetadata)
        {
            return;
        }

        mutableMetadata[PrMetadataKeys.AttentionScope] = attentionScope;

        if (string.Equals(fallback, "displayName", StringComparison.Ordinal))
        {
            mutableMetadata[PrMetadataKeys.AttentionScopeFallback] = "displayName";
            return;
        }

        mutableMetadata.Remove(PrMetadataKeys.AttentionScopeFallback);
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
