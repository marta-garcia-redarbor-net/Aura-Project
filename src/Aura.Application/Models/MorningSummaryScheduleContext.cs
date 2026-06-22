namespace Aura.Application.Models;

/// <summary>
/// Scheduler context contract for resolving a Morning Summary window.
/// </summary>
/// <param name="UserId">Target user identifier.</param>
/// <param name="UserTimeZoneId">User timezone identifier.</param>
/// <param name="TargetLocalTime">Target local schedule time.</param>
/// <param name="WindowDate">Local date used for resolution.</param>
public sealed record MorningSummaryScheduleContext(
    string UserId,
    string UserTimeZoneId,
    TimeOnly TargetLocalTime,
    DateOnly WindowDate);
