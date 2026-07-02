using Aura.Application.Ports;
using Aura.Application.Models;
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

    public async Task<IReadOnlyList<UpcomingMeetingDto>> ExecuteAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var events = await _store.GetUpcomingAsync(from, to, ct);

        return events
            .OrderBy(e => e.StartUtc)
            .Select(e => new UpcomingMeetingDto(
                e.Id,
                e.Title,
                e.StartUtc,
                e.EndUtc,
                e.IsOnlineMeeting,
                e.JoinUrl,
                e.Organizer,
                e.Location))
            .ToList();
    }
}
