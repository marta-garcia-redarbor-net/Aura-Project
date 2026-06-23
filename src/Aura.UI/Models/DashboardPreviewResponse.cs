namespace Aura.UI.Models;

public sealed record DashboardPreviewResponse(
    IReadOnlyList<InboxSourceGroupResponse> InboxGroups,
    IReadOnlyList<SummaryPreviewEntryResponse> SummaryEntries);

public sealed record InboxSourceGroupResponse(
    string Source,
    IReadOnlyList<InboxItemPreviewResponse> Items);

public sealed record InboxItemPreviewResponse(
    string Title,
    string Source,
    string RelativeTimestamp,
    double Score,
    string SuggestedAction);

public sealed record SummaryPreviewEntryResponse(
    int Rank,
    string Title,
    string Source,
    double Score);
