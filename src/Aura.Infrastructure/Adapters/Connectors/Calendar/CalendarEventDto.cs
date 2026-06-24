namespace Aura.Infrastructure.Adapters.Connectors.Calendar;

internal sealed class CalendarEventDto
{
    public string? ExternalId { get; init; }
    public string? Subject { get; init; }
    public DateTimeOffset? Start { get; init; }
    public DateTimeOffset? End { get; init; }
    public bool IsOnlineMeeting { get; init; }
    public string? JoinUrl { get; init; }
    public string? OrganizerName { get; init; }
    public string? OrganizerAddress { get; init; }
    public string? LocationDisplayName { get; init; }
    public bool IsCancelled { get; init; }
    public string? OriginalTimeZone { get; init; }
}