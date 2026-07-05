namespace Aura.Domain.Calendar;

public sealed record CalendarEvent(
    string Id,
    string Title,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    bool IsOnlineMeeting,
    string? JoinUrl = null,
    string? Organizer = null,
    string? Location = null,
    string? OriginalTimeZone = null,
    string? UserId = null);