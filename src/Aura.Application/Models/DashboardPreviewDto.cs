namespace Aura.Application.Models;

/// <summary>
/// Dashboard preview payload composed for inbox-by-source and morning summary preview.
/// </summary>
public sealed record DashboardPreviewDto(
    IReadOnlyList<InboxSourceGroupDto> InboxGroups,
    IReadOnlyList<SummaryPreviewEntryDto> SummaryEntries);

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
    public string? Sender { get; init; }
    public string? Snippet { get; init; }
    public string? DeepLink { get; init; }
    public string? PriorityHint { get; init; }
    public string? SyncState { get; init; }
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
