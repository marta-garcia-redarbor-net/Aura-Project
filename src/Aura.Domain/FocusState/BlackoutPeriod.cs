namespace Aura.Domain.FocusState;

/// <summary>
/// Value object representing a recurring blackout period — a time window
/// during which the user's focus state is forced to <see cref="FocusStateType.DeepWork"/>
/// or <see cref="FocusStateType.Away"/>.
/// </summary>
public sealed record BlackoutPeriod
{
    /// <summary>
    /// Initializes a new instance of <see cref="BlackoutPeriod"/> with validation.
    /// </summary>
    /// <param name="Label">A human-readable label for this blackout period.</param>
    /// <param name="TargetState">The focus state to enforce during this period (DeepWork or Away).</param>
    /// <param name="StartTime">The start time of the blackout period (inclusive, local time).</param>
    /// <param name="EndTime">The end time of the blackout period (exclusive, local time).</param>
    /// <param name="DaysOfWeek">The days of the week this blackout applies to. Must be non-empty.</param>
    /// <param name="TimeZoneId">The IANA or Windows time zone ID for interpreting StartTime/EndTime.</param>
    public BlackoutPeriod(
        string Label,
        FocusStateType TargetState,
        TimeOnly StartTime,
        TimeOnly EndTime,
        IReadOnlyList<DayOfWeek> DaysOfWeek,
        string TimeZoneId)
    {
        Validate(TargetState, StartTime, EndTime, DaysOfWeek);

        this.Label = Label;
        this.TargetState = TargetState;
        this.StartTime = StartTime;
        this.EndTime = EndTime;
        this.DaysOfWeek = DaysOfWeek;
        this.TimeZoneId = TimeZoneId;
    }

    /// <summary>A human-readable label for this blackout period.</summary>
    public string Label { get; init; }

    /// <summary>The focus state to enforce during this period (DeepWork or Away).</summary>
    public FocusStateType TargetState { get; init; }

    /// <summary>The start time of the blackout period (inclusive, local time).</summary>
    public TimeOnly StartTime { get; init; }

    /// <summary>The end time of the blackout period (exclusive, local time).</summary>
    public TimeOnly EndTime { get; init; }

    /// <summary>The days of the week this blackout applies to. Must be non-empty.</summary>
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; init; }

    /// <summary>The IANA or Windows time zone ID for interpreting StartTime/EndTime.</summary>
    public string TimeZoneId { get; init; }

    /// <summary>
    /// Validates invariants: StartTime must precede EndTime, DaysOfWeek must be non-empty.
    /// </summary>
    private static void Validate(FocusStateType targetState, TimeOnly startTime, TimeOnly endTime, IReadOnlyList<DayOfWeek> daysOfWeek)
    {
        if (targetState is not (FocusStateType.DeepWork or FocusStateType.Away))
            throw new ArgumentException(
                "BlackoutPeriod: TargetState must be DeepWork or Away.",
                nameof(targetState));

        if (startTime >= endTime)
            throw new ArgumentException(
                $"BlackoutPeriod: StartTime ({startTime}) must precede EndTime ({endTime}).",
                nameof(startTime));

        if (daysOfWeek.Count == 0)
            throw new ArgumentException(
                "BlackoutPeriod: DaysOfWeek must contain at least one day.",
                nameof(daysOfWeek));
    }

    /// <summary>
    /// Determines whether this blackout period is active at the given UTC instant.
    /// Conversion from UTC to local time uses <see cref="TimeZoneInfo"/> with the configured <see cref="TimeZoneId"/>.
    /// Falls back to UTC if the time zone cannot be resolved.
    /// </summary>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <param name="timeProvider">Time provider (reserved for future TimeProvider-based TZ resolution).</param>
    /// <returns><c>true</c> if the current local time falls within the blackout window on a matching day.</returns>
    public bool IsActive(DateTimeOffset utcNow, TimeProvider timeProvider)
    {
        _ = timeProvider;

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId ?? "UTC");
        }
        catch
        {
            tz = TimeZoneInfo.Utc;
        }

        var localTime = TimeZoneInfo.ConvertTime(utcNow, tz);
        var localTimeOnly = TimeOnly.FromDateTime(localTime.DateTime);
        var localDayOfWeek = localTime.DayOfWeek;

        if (!DaysOfWeek.Contains(localDayOfWeek))
            return false;

        return localTimeOnly >= StartTime && localTimeOnly < EndTime;
    }
}
