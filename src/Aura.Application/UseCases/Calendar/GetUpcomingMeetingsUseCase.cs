using Aura.Application.Ports;
using Aura.Domain.Calendar;

namespace Aura.Application.UseCases.Calendar;

public class GetUpcomingMeetingsUseCase
{
    private readonly ICalendarEventStore _store;

    public GetUpcomingMeetingsUseCase(ICalendarEventStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public async Task<IReadOnlyList<CalendarEvent>> ExecuteAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var events = await _store.GetUpcomingAsync(from, to, ct);
        return events.OrderBy(e => e.StartUtc).ToList();
    }
}