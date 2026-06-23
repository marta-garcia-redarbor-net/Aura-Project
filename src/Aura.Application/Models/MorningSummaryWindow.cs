namespace Aura.Application.Models;

/// <summary>
/// Window contract for Morning Summary scheduling and composition.
/// </summary>
/// <param name="WindowDate">Local date represented by the summary window.</param>
/// <param name="UserTimeZoneId">User timezone identifier for the window.</param>
/// <param name="ScheduledLocalTime">Target local time for the summary window.</param>
/// <param name="ScheduledInstantUtc">UTC instant corresponding to the scheduled local time.</param>
public sealed record MorningSummaryWindow(
    DateOnly WindowDate,
    string UserTimeZoneId,
    TimeOnly ScheduledLocalTime,
    DateTimeOffset ScheduledInstantUtc);
