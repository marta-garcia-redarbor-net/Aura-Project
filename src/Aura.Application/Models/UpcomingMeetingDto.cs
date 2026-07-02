namespace Aura.Application.Models;

public sealed record UpcomingMeetingDto(
    string Id,
    string Title,
    DateTimeOffset StartUtc,
    DateTimeOffset EndUtc,
    bool IsOnlineMeeting,
    string? JoinUrl,
    string? Organizer,
    string? Location);
