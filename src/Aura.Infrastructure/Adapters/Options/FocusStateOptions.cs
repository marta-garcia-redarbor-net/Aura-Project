namespace Aura.Infrastructure.Adapters.Options;

/// <summary>
/// Configuration options for focus state resolution.
/// Bound from the <c>FocusState</c> section in appsettings.json.
/// </summary>
public sealed class FocusStateOptions
{
    /// <summary>Configuration section name in appsettings.json.</summary>
    public const string SectionName = "FocusState";

    /// <summary>Start of working hours (inclusive, local time). Default: 08:00.</summary>
    public TimeOnly WorkingHoursStart { get; set; } = new(8, 0);

    /// <summary>End of working hours (exclusive, local time). Default: 18:00.</summary>
    public TimeOnly WorkingHoursEnd { get; set; } = new(18, 0);

    /// <summary>Minutes before/after a calendar event to treat as busy. Default: 5.</summary>
    public int MeetingBufferMinutes { get; set; } = 5;

    /// <summary>Recurring blackout periods (e.g. DeepWork blocks, lunch).</summary>
    public IReadOnlyList<BlackoutPeriodDto> BlackoutPeriods { get; set; } = [];
}
