using Aura.UI.Models;

namespace Aura.UI.Services;

public sealed record PrioritySummaryCard(
    string DisplayName,
    string Icon,
    string CssClass,
    string CountLabel,
    string ItemsLabel,
    string SourceLabel,
    string ViewAllUrl,
    string? DetailPageUrl,
    List<InboxItemPreviewResponse>? PreviewItems,
    List<UpcomingMeetingResponse>? CalendarItems)
{
    public bool IsCalendarCard => CalendarItems is not null;
    public bool IsPrCard { get; init; }
    public List<PrPreviewItemResponse>? PrItems { get; init; }
    public int TotalCount => PrItems?.Count ?? PreviewItems?.Count ?? CalendarItems?.Count ?? 0;
}

public interface IPrioritySummaryService
{
    Task<List<PrioritySummaryCard>> GetCardsAsync(CancellationToken cancellationToken = default);
    string FormatTimeRange(UpcomingMeetingResponse ev);
    string GetEventStatus(UpcomingMeetingResponse ev);
}
