using Aura.Application.Ports;
using Aura.Domain.Calendar;

namespace Aura.Infrastructure.Adapters.Connectors.Calendar;

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

    public Task<IReadOnlyList<CalendarEvent>> GetUpcomingAsync(string userId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ct.ThrowIfCancellationRequested();

        var upcoming = _events.Values
            .Where(e => string.Equals(e.UserId, userId, StringComparison.OrdinalIgnoreCase))
            .Where(e => e.StartUtc <= to && e.EndUtc >= from)
            .OrderBy(e => e.StartUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<CalendarEvent>>(upcoming);
    }

    public Task ClearDemoEventsAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var demoIds = _events.Keys
            .Where(id => id.StartsWith("demo-", StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var id in demoIds)
            _events.Remove(id);
        return Task.CompletedTask;
    }
}
