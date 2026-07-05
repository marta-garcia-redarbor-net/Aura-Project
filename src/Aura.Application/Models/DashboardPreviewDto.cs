namespace Aura.Application.Models;

/// <summary>
/// Dashboard preview payload composed for inbox-by-source and morning summary preview.
/// </summary>
public sealed record DashboardPreviewDto(
    IReadOnlyList<InboxSourceGroupDto> InboxGroups,
    IReadOnlyList<SummaryPreviewEntryDto> SummaryEntries)
{
    /// <summary>Total count of pending items across all inbox groups.</summary>
    public int TotalPendingCount { get; init; }

    /// <summary>Count of pending items considered high priority (effective score &gt;= 75).</summary>
    public int HighPriorityCount { get; init; }

    /// <summary>Top 3 highest-priority items by PriorityScore DESC.</summary>
    public IReadOnlyList<InboxItemPreviewDto> TopItems { get; init; } = Array.Empty<InboxItemPreviewDto>();
}

/// <summary>
/// Source-keyed inbox group for dashboard preview rendering.
/// </summary>
public sealed record InboxSourceGroupDto(
    string Source,
    IReadOnlyList<InboxItemPreviewDto> Items);

/// <summary>
/// Slim inbox item projection for dashboard preview cards.
/// </summary>
public sealed record InboxItemPreviewDto(
    string Title,
    string Source,
    string RelativeTimestamp,
    double Score,
    string SuggestedAction)
{
    public DateTimeOffset CapturedAtUtc { get; init; }
    public string? Sender { get; init; }
    public string? Snippet { get; init; }
    public string? DeepLink { get; init; }
    public string? PriorityHint { get; init; }
    public string? SyncState { get; init; }
    public int? PriorityScore { get; init; }

    /// <summary>Number of unread messages (applicable to chat-based sources).</summary>
    public int? UnreadCount { get; init; }
}

/// <summary>
/// Slim morning summary preview entry.
/// </summary>
public sealed record SummaryPreviewEntryDto(
    int Rank,
    string Title,
    string Source,
    double Score)
{
    public string? PriorityHint { get; init; }
}
