namespace Aura.Domain.Calendar;

public sealed record MeetingAlert(
    string EventId,
    string Title,
    MeetingAlertTrigger Trigger,
    DateTimeOffset StartsAtUtc,
    string? JoinUrl,
    string UserId,
    bool HasBeenSent = false);
