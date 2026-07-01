using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Application.Services;

/// <summary>
/// Projects ranked work items into dashboard-specific preview DTOs.
/// </summary>
public sealed class DashboardPreviewReader : IDashboardPreviewReader
{
    private readonly IWorkItemReader? _workItemReader;
    private readonly IMorningSummaryRankingPolicy _rankingPolicy;
    private readonly ICurrentUserService _currentUserService;
    private readonly Func<DateTimeOffset> _utcNow;

    public DashboardPreviewReader(
        IWorkItemReader workItemReader,
        IMorningSummaryRankingPolicy rankingPolicy,
        ICurrentUserService currentUserService,
        Func<DateTimeOffset>? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(workItemReader);
        ArgumentNullException.ThrowIfNull(rankingPolicy);
        ArgumentNullException.ThrowIfNull(currentUserService);

        _workItemReader = workItemReader;
        _rankingPolicy = rankingPolicy;
        _currentUserService = currentUserService;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public DashboardPreviewReader(
        IMorningSummaryRankingPolicy rankingPolicy,
        ICurrentUserService currentUserService,
        Func<DateTimeOffset>? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(rankingPolicy);
        ArgumentNullException.ThrowIfNull(currentUserService);

        _rankingPolicy = rankingPolicy;
        _currentUserService = currentUserService;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<DashboardPreviewDto> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var now = _utcNow();
        var currentUser = _currentUserService.GetCurrentUser();
        var userId = string.IsNullOrWhiteSpace(currentUser?.UserId) ? "system" : currentUser.UserId;

        var query = new MorningSummaryQuery(userId, now.AddHours(-24), now);
        var items = _workItemReader is null
            ? []
            : await _workItemReader.ReadForWindowAsync(query, Domain.WorkItems.WorkItemStatus.Pending, cancellationToken);
        var ranked = _rankingPolicy.Rank(items);

        var groups = ranked
            .GroupBy(entry => entry.Item.Source, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new InboxSourceGroupDto(
                group.Key,
                group.Select(entry => new InboxItemPreviewDto(
                    entry.Item.Title,
                    entry.Item.Source,
                    ToRelativeTimestamp(entry.Item.CapturedAtUtc, now),
                    entry.Score,
                    BuildSuggestedAction(entry.Item.Source))
                {
                    Sender = ExtractMetadata(entry.Item, "sender"),
                    Snippet = ExtractMetadata(entry.Item, "snippet"),
                    DeepLink = ExtractMetadata(entry.Item, "deepLink"),
                    PriorityHint = entry.Item.Priority.ToString(),
                    SyncState = HasSyncedMetadata(entry.Item) ? "synced" : null,
                    UnreadCount = ParseUnreadCount(entry.Item)
                })
                .ToArray()))
            .ToArray();

        var summary = ranked
            .Select(entry => new SummaryPreviewEntryDto(
                entry.Rank,
                entry.Item.Title,
                entry.Item.Source,
                entry.Score))
            .ToArray();

        return new DashboardPreviewDto(groups, summary);
    }

    private static string BuildSuggestedAction(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return "Review and triage";
        }

        return source.Trim().ToLowerInvariant() switch
        {
            "outlook" => "Review and reply",
            "teams" => "Review and respond",
            "github" => "Review and prioritize",
            _ => "Review and triage"
        };
    }

    /// <summary>
    /// Extracts a metadata value using source-prefixed keys.
    /// Tries "{source}.{field}" first (e.g. "teams.sender", "outlook.sender").
    /// </summary>
    private static string? ExtractMetadata(Domain.WorkItems.WorkItem item, string field)
    {
        var sourceKey = $"{item.Source.Trim().ToLowerInvariant()}.{field}";
        if (item.Metadata.TryGetValue(sourceKey, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return null;
    }

    /// <summary>
    /// Determines if a work item has synced metadata (sender, snippet, or deepLink present).
    /// </summary>
    /// <summary>
    /// Parses the UnreadCount from metadata using source-prefixed key "{source}.unreadCount".
    /// Returns 0 when the key is absent or the value is not a valid integer.
    /// </summary>
    private static int? ParseUnreadCount(Domain.WorkItems.WorkItem item)
    {
        var sourceKey = $"{item.Source.Trim().ToLowerInvariant()}.unreadCount";
        if (item.Metadata.TryGetValue(sourceKey, out var value)
            && int.TryParse(value, out var count))
        {
            return count;
        }

        return 0;
    }

    private static bool HasSyncedMetadata(Domain.WorkItems.WorkItem item)
    {
        return ExtractMetadata(item, "sender") is not null
            || ExtractMetadata(item, "snippet") is not null
            || ExtractMetadata(item, "deepLink") is not null;
    }

    private static string ToRelativeTimestamp(DateTimeOffset capturedAtUtc, DateTimeOffset nowUtc)
    {
        var elapsed = nowUtc - capturedAtUtc;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        if (elapsed.TotalMinutes < 1)
        {
            return "just now";
        }

        if (elapsed.TotalHours < 1)
        {
            return $"{(int)elapsed.TotalMinutes}m ago";
        }

        if (elapsed.TotalDays < 1)
        {
            return $"{(int)elapsed.TotalHours}h ago";
        }

        return $"{(int)elapsed.TotalDays}d ago";
    }
}
