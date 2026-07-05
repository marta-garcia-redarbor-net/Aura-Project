using Aura.Application.Ports;
using Aura.Domain.Calendar;

namespace Aura.Application.UseCases.Calendar;

public class CheckAndDispatchMeetingAlertsUseCase
{
    private readonly ICalendarEventStore _eventStore;
    private readonly IMeetingAlertStore _alertStore;
    private readonly IMeetingAlertDispatcher _dispatcher;

    private static readonly (MeetingAlertTrigger Trigger, TimeSpan BeforeStart)[] Triggers =
    [
        (MeetingAlertTrigger.SixtyMinutes, TimeSpan.FromMinutes(60)),
        (MeetingAlertTrigger.TenMinutes, TimeSpan.FromMinutes(10)),
        (MeetingAlertTrigger.FiveMinutes, TimeSpan.FromMinutes(5))
    ];

    public CheckAndDispatchMeetingAlertsUseCase(
        ICalendarEventStore eventStore,
        IMeetingAlertStore alertStore,
        IMeetingAlertDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentNullException.ThrowIfNull(alertStore);
        ArgumentNullException.ThrowIfNull(dispatcher);

        _eventStore = eventStore;
        _alertStore = alertStore;
        _dispatcher = dispatcher;
    }

    public async Task ExecuteAsync(string userId, DateTimeOffset now, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        // Fetch events in a 61-minute window ahead (max trigger is 60 min)
        var windowEnd = now.AddMinutes(61);
        var events = await _eventStore.GetUpcomingAsync(userId, now, windowEnd, ct);

        foreach (var calendarEvent in events)
        {
            foreach (var (trigger, beforeStart) in Triggers)
            {
                var triggerTime = calendarEvent.StartUtc - beforeStart;

                // Only fire if trigger time is at or before now, but event hasn't started yet
                if (triggerTime > now || calendarEvent.StartUtc <= now)
                    continue;

                var existingAlert = await _alertStore.GetUnsentAlertAsync(
                    calendarEvent.Id, trigger, now.Date, ct);

                if (existingAlert is not null)
                    continue;

                var alert = new MeetingAlert(
                    calendarEvent.Id,
                    calendarEvent.Title,
                    trigger,
                    calendarEvent.StartUtc,
                    calendarEvent.JoinUrl,
                    UserId: userId,
                    HasBeenSent: false);

                await _dispatcher.DispatchAsync(alert, ct);
                await _alertStore.MarkSentAsync(alert, ct);
            }
        }
    }
}
