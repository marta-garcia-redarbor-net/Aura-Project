using Aura.Domain.Calendar;

namespace Aura.Infrastructure.Adapters.Connectors.Calendar;

internal sealed class CalendarEventMapper
{
    public bool TryMap(CalendarEventDto dto, out CalendarEvent? calendarEvent)
    {
        calendarEvent = null;

        if (string.IsNullOrWhiteSpace(dto.ExternalId) || string.IsNullOrWhiteSpace(dto.Subject))
        {
            return false;
        }

        if (dto.IsCancelled)
        {
            return false;
        }

        if (!dto.Start.HasValue || !dto.End.HasValue)
        {
            return false;
        }

        calendarEvent = new CalendarEvent(
            Id: dto.ExternalId,
            Title: dto.Subject,
            StartUtc: dto.Start.Value.UtcDateTime,
            EndUtc: dto.End.Value.UtcDateTime,
            IsOnlineMeeting: dto.IsOnlineMeeting,
            JoinUrl: dto.JoinUrl,
            Organizer: dto.OrganizerName,
            Location: dto.LocationDisplayName,
            OriginalTimeZone: dto.OriginalTimeZone);

        return true;
    }
}