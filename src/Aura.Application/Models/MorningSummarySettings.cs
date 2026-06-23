namespace Aura.Application.Models;

public sealed record MorningSummarySettings(string? TimezoneId, TimeOnly TargetLocalTime);
