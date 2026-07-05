using Aura.Domain.Calendar;

namespace Aura.Application.Ports;

public interface ICalendarEventStore
{
    Task SaveAsync(CalendarEvent calendarEvent, CancellationToken ct);
    Task SaveBatchAsync(IReadOnlyList<CalendarEvent> events, CancellationToken ct);
    Task<IReadOnlyList<CalendarEvent>> GetUpcomingAsync(string userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
}
