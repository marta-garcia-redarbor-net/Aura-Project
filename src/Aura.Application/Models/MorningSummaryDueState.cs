namespace Aura.Application.Models;

public sealed record MorningSummaryDueState(
    bool IsDue,
    string ResolvedTimezoneId,
    DateOnly LocalDate,
    TimeOnly TargetLocalTime);
