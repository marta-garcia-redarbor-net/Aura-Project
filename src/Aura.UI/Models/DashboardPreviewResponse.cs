namespace Aura.UI.Models;

public sealed record DashboardPreviewResponse(
    IReadOnlyList<InboxSourceGroupResponse> InboxGroups,
    IReadOnlyList<SummaryPreviewEntryResponse> SummaryEntries)
{
    public int TotalPendingCount { get; init; }
    public int HighPriorityCount { get; init; }
    public IReadOnlyList<InboxItemPreviewResponse> TopItems { get; init; } = Array.Empty<InboxItemPreviewResponse>();
}

public sealed record InboxSourceGroupResponse(
    string Source,
    IReadOnlyList<InboxItemPreviewResponse> Items);

public sealed record InboxItemPreviewResponse(
    string Title,
    string Source,
    string RelativeTimestamp,
    double Score,
    string SuggestedAction)
{
    public string? Sender { get; init; }
    public string? Snippet { get; init; }
    public string? DeepLink { get; init; }
    public string? PriorityHint { get; init; }
    public string? SyncState { get; init; }
    public int? PriorityScore { get; init; }
}

public sealed record SummaryPreviewEntryResponse(
    int Rank,
    string Title,
    string Source,
    double Score)
{
    public string? PriorityHint { get; init; }
}
