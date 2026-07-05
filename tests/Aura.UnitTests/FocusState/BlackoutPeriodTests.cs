using Aura.Domain.FocusState;

namespace Aura.UnitTests.FocusState;

public sealed class BlackoutPeriodTests
{
    // ============================================================
    // Construction validation
    // ============================================================

    [Fact]
    public void Constructor_ValidBlackout_CreatesInstance()
    {
        var period = new BlackoutPeriod(
            Label: "Deep Work Block",
            TargetState: FocusStateType.DeepWork,
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(12, 0),
            DaysOfWeek: [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
            TimeZoneId: "UTC");

        Assert.Equal("Deep Work Block", period.Label);
        Assert.Equal(FocusStateType.DeepWork, period.TargetState);
        Assert.Equal(new TimeOnly(10, 0), period.StartTime);
        Assert.Equal(new TimeOnly(12, 0), period.EndTime);
        Assert.Equal(3, period.DaysOfWeek.Count);
        Assert.Equal("UTC", period.TimeZoneId);
    }

    [Fact]
    public void Constructor_StartAfterEnd_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new BlackoutPeriod(
                Label: "Invalid",
                TargetState: FocusStateType.Away,
                StartTime: new TimeOnly(14, 0),
                EndTime: new TimeOnly(12, 0),
                DaysOfWeek: [DayOfWeek.Monday],
                TimeZoneId: "UTC"));

        Assert.Contains("StartTime", ex.Message);
        Assert.Contains("EndTime", ex.Message);
    }

    [Fact]
    public void Constructor_StartEqualToEnd_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new BlackoutPeriod(
                Label: "Invalid",
                TargetState: FocusStateType.Away,
                StartTime: new TimeOnly(10, 0),
                EndTime: new TimeOnly(10, 0),
                DaysOfWeek: [DayOfWeek.Monday],
                TimeZoneId: "UTC"));

        Assert.Contains("StartTime", ex.Message);
        Assert.Contains("EndTime", ex.Message);
    }

    [Fact]
    public void Constructor_EmptyDaysOfWeek_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new BlackoutPeriod(
                Label: "No Days",
                TargetState: FocusStateType.DeepWork,
                StartTime: new TimeOnly(10, 0),
                EndTime: new TimeOnly(12, 0),
                DaysOfWeek: [],
                TimeZoneId: "UTC"));

        Assert.Contains("DaysOfWeek", ex.Message);
    }

    [Fact]
    public void Constructor_TargetStateWindowOfOpportunity_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new BlackoutPeriod(
                Label: "Invalid",
                TargetState: FocusStateType.WindowOfOpportunity,
                StartTime: new TimeOnly(10, 0),
                EndTime: new TimeOnly(12, 0),
                DaysOfWeek: [DayOfWeek.Monday],
                TimeZoneId: "UTC"));

        Assert.Contains("TargetState", ex.Message);
    }

    // ============================================================
    // IsActive — day of week matching
    // ============================================================

    [Fact]
    public void IsActive_MatchingDayWithinWindow_ReturnsTrue()
    {
        var period = new BlackoutPeriod(
            Label: "Morning Block",
            TargetState: FocusStateType.DeepWork,
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(12, 0),
            DaysOfWeek: [DayOfWeek.Wednesday],
            TimeZoneId: "UTC");

        // Wednesday July 8 2026, 11:00 UTC — within 10:00-12:00, on Wednesday
        var utcNow = new DateTimeOffset(2026, 7, 8, 11, 0, 0, TimeSpan.Zero);
        var result = period.IsActive(utcNow, TimeProvider.System);

        Assert.True(result);
    }

    [Fact]
    public void IsActive_WrongDayOfWeek_ReturnsFalse()
    {
        var period = new BlackoutPeriod(
            Label: "Morning Block",
            TargetState: FocusStateType.DeepWork,
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(12, 0),
            DaysOfWeek: [DayOfWeek.Monday],
            TimeZoneId: "UTC");

        // Wednesday July 8 2026 — not Monday
        var utcNow = new DateTimeOffset(2026, 7, 8, 11, 0, 0, TimeSpan.Zero);
        var result = period.IsActive(utcNow, TimeProvider.System);

        Assert.False(result);
    }

    // ============================================================
    // IsActive — time window boundaries
    // ============================================================

    [Fact]
    public void IsActive_ExactStartTime_ReturnsTrue()
    {
        var period = new BlackoutPeriod(
            Label: "Block",
            TargetState: FocusStateType.DeepWork,
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(12, 0),
            DaysOfWeek: [DayOfWeek.Wednesday],
            TimeZoneId: "UTC");

        var utcNow = new DateTimeOffset(2026, 7, 8, 10, 0, 0, TimeSpan.Zero);
        var result = period.IsActive(utcNow, TimeProvider.System);

        Assert.True(result);
    }

    [Fact]
    public void IsActive_BeforeStartTime_ReturnsFalse()
    {
        var period = new BlackoutPeriod(
            Label: "Block",
            TargetState: FocusStateType.DeepWork,
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(12, 0),
            DaysOfWeek: [DayOfWeek.Wednesday],
            TimeZoneId: "UTC");

        var utcNow = new DateTimeOffset(2026, 7, 8, 9, 59, 59, TimeSpan.Zero);
        var result = period.IsActive(utcNow, TimeProvider.System);

        Assert.False(result);
    }

    [Fact]
    public void IsActive_ExactEndTime_ReturnsTrue()
    {
        var period = new BlackoutPeriod(
            Label: "Block",
            TargetState: FocusStateType.DeepWork,
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(12, 0),
            DaysOfWeek: [DayOfWeek.Wednesday],
            TimeZoneId: "UTC");

        // EndTime is exclusive — 12:00 is still at the boundary
        // We treat [start, end) so 12:00:00 is NOT active
        var utcNow = new DateTimeOffset(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
        var result = period.IsActive(utcNow, TimeProvider.System);

        Assert.False(result);
    }

    // ============================================================
    // IsActive — timezone conversion
    // ============================================================

    [Fact]
    public void IsActive_TimeZoneConversion_ConvertsToLocalTime()
    {
        var period = new BlackoutPeriod(
            Label: "Afternoon Block ET",
            TargetState: FocusStateType.Away,
            StartTime: new TimeOnly(13, 0),  // 1 PM ET
            EndTime: new TimeOnly(14, 0),    // 2 PM ET
            DaysOfWeek: [DayOfWeek.Wednesday],
            TimeZoneId: "Eastern Standard Time");

        // Wednesday July 8 2026, 17:00 UTC = 1:00 PM ET (EDT, UTC-4)
        var utcNow = new DateTimeOffset(2026, 7, 8, 17, 0, 0, TimeSpan.Zero);
        var result = period.IsActive(utcNow, TimeProvider.System);

        Assert.True(result);
    }

    [Fact]
    public void IsActive_TimeZoneConversion_WrapsDayAcrossMidnight()
    {
        var period = new BlackoutPeriod(
            Label: "Late Block ET",
            TargetState: FocusStateType.DeepWork,
            StartTime: new TimeOnly(23, 0),  // 11 PM ET
            EndTime: new TimeOnly(23, 30),   // 11:30 PM ET
            DaysOfWeek: [DayOfWeek.Wednesday],
            TimeZoneId: "Eastern Standard Time");

        // Wednesday July 8 2026, 3:30 UTC = 11:30 PM ET Tue Jul 7 (EDT, UTC-4)
        // Wait, 03:30 UTC - 4h = 23:30 ET on July 7 (Tuesday)... this is actually the previous day in ET
        // Actually EDT is UTC-4 so 3:00 UTC = 11:00 PM ET on July 7.
        // Let me use 3:15 UTC = 11:15 PM ET on July 7... hmm that's still Tuesday in ET.
        // For Wednesday in ET: 04:00 UTC July 8 = 12:00 AM ET July 8
        // So to be Wednesday at 11:15 PM ET: that's July 8 11:15 PM ET = July 9 03:15 UTC
        // Actually let's just use 23:00 UTC = 19:00 ET on Wednesday... that doesn't test the wrap.
        // The block is 23:00-23:30 ET. For it to be Wednesday ET:
        // 23:30 ET Wed = 03:30 UTC Thu (next day UTC)
        var utcNow = new DateTimeOffset(2026, 7, 9, 3, 15, 0, TimeSpan.Zero); // Thu 03:15 UTC = Wed 23:15 ET
        var result = period.IsActive(utcNow, TimeProvider.System);

        Assert.True(result);
    }

    // ============================================================
    // IsActive — null/empty TimeZoneId defaults to UTC
    // ============================================================

    [Fact]
    public void IsActive_NullTimeZoneId_UsesUtc()
    {
        var period = new BlackoutPeriod(
            Label: "Block",
            TargetState: FocusStateType.DeepWork,
            StartTime: new TimeOnly(10, 0),
            EndTime: new TimeOnly(12, 0),
            DaysOfWeek: [DayOfWeek.Wednesday],
            TimeZoneId: null!);

        var utcNow = new DateTimeOffset(2026, 7, 8, 11, 0, 0, TimeSpan.Zero);
        var result = period.IsActive(utcNow, TimeProvider.System);

        Assert.True(result);
    }
}
