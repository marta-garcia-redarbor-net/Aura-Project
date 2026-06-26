using Aura.Application.Ports;
using Aura.Domain.Calendar;

namespace Aura.UI.Services;

/// <summary>
/// In-memory implementation of <see cref="ICalendarEventStore"/> for the Blazor UI.
/// Used for dashboard display — stores calendar events in a dictionary keyed by event ID.
/// </summary>
internal sealed class InMemoryCalendarEventStore : ICalendarEventStore
{
    private readonly Dictionary<string, CalendarEvent> _events = new();

    public Task SaveAsync(CalendarEvent calendarEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);
        ct.ThrowIfCancellationRequested();

        _events[calendarEvent.Id] = calendarEvent;
        return Task.CompletedTask;
    }

    public Task SaveBatchAsync(IReadOnlyList<CalendarEvent> events, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(events);
        ct.ThrowIfCancellationRequested();

        foreach (var calendarEvent in events)
        {
            _events[calendarEvent.Id] = calendarEvent;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CalendarEvent>> GetUpcomingAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var upcoming = _events.Values
            .Where(e => e.StartUtc >= from && e.StartUtc <= to)
            .OrderBy(e => e.StartUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<CalendarEvent>>(upcoming);
    }
}
