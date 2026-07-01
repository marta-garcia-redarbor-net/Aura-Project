namespace Aura.UI.Models;

public sealed record UpcomingMeetingResponse(
    string Id,
    string Title,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    bool IsOnlineMeeting,
    string? JoinUrl,
    string? Organizer,
    string? Location);
