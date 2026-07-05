namespace Aura.Infrastructure.Adapters.Options;

/// <summary>
/// Config DTO for a single blackout period, deserialized from the <c>FocusState</c> configuration section.
/// Maps to <see cref="Aura.Domain.FocusState.BlackoutPeriod"/> after validation.
/// </summary>
public sealed class BlackoutPeriodDto
{
    /// <summary>A human-readable label (e.g. "Deep Work Block", "Lunch").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>The focus state to enforce: <c>DeepWork</c> or <c>Away</c>.</summary>
    public string TargetState { get; set; } = "DeepWork";

    /// <summary>Start time of the blackout window (inclusive, local time).</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>End time of the blackout window (exclusive, local time).</summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>Days of the week this blackout applies to.</summary>
    public List<DayOfWeek> DaysOfWeek { get; set; } = [];

    /// <summary>Time zone ID (IANA or Windows) for interpreting StartTime/EndTime.</summary>
    public string TimeZoneId { get; set; } = "UTC";
}
