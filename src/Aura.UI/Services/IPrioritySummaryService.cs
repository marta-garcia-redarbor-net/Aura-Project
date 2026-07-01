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
    public int TotalCount => PreviewItems?.Count ?? CalendarItems?.Count ?? 0;
}

public interface IPrioritySummaryService
{
    Task<List<PrioritySummaryCard>> GetCardsAsync(CancellationToken cancellationToken = default);
    string FormatTimeRange(UpcomingMeetingResponse ev);
    string GetEventStatus(UpcomingMeetingResponse ev);
}
