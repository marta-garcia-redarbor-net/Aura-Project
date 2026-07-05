using System.Text.Json;
using Aura.Infrastructure.Adapters.Options;

namespace Aura.UnitTests.Infrastructure;

public sealed class FocusStateOptionsTests
{
    [Fact]
    public void Bind_FromJson_PopulatesAllProperties()
    {
        var json = """
            {
                "WorkingHoursStart": "09:00",
                "WorkingHoursEnd": "17:00",
                "MeetingBufferMinutes": 10,
                "BlackoutPeriods": [
                    {
                        "Label": "Deep Work Morning",
                        "TargetState": "DeepWork",
                        "StartTime": "10:00",
                        "EndTime": "12:00",
                        "DaysOfWeek": [1, 3, 5],
                        "TimeZoneId": "Eastern Standard Time"
                    }
                ]
            }
            """;

        var options = JsonSerializer.Deserialize<FocusStateOptions>(json);

        Assert.NotNull(options);
        Assert.Equal(new TimeOnly(9, 0), options!.WorkingHoursStart);
        Assert.Equal(new TimeOnly(17, 0), options.WorkingHoursEnd);
        Assert.Equal(10, options.MeetingBufferMinutes);

        Assert.Single(options.BlackoutPeriods);
        var bp = options.BlackoutPeriods[0];
        Assert.Equal("Deep Work Morning", bp.Label);
        Assert.Equal("DeepWork", bp.TargetState);
        Assert.Equal(new TimeOnly(10, 0), bp.StartTime);
        Assert.Equal(new TimeOnly(12, 0), bp.EndTime);
        Assert.Equal(3, bp.DaysOfWeek.Count);
        Assert.Contains(DayOfWeek.Monday, bp.DaysOfWeek);
        Assert.Contains(DayOfWeek.Wednesday, bp.DaysOfWeek);
        Assert.Contains(DayOfWeek.Friday, bp.DaysOfWeek);
        Assert.Equal("Eastern Standard Time", bp.TimeZoneId);
    }

    [Fact]
    public void Defaults_WhenJsonMissingProperties()
    {
        var json = "{}";

        var options = JsonSerializer.Deserialize<FocusStateOptions>(json);

        Assert.NotNull(options);
        Assert.Equal(new TimeOnly(8, 0), options!.WorkingHoursStart);
        Assert.Equal(new TimeOnly(18, 0), options.WorkingHoursEnd);
        Assert.Equal(5, options.MeetingBufferMinutes);
        Assert.Empty(options.BlackoutPeriods);
    }

    [Fact]
    public void BlackoutPeriodDto_Defaults_ToEmptyAndUtc()
    {
        var json = """{ "BlackoutPeriods": [{ "Label": "Test" }] }""";

        var options = JsonSerializer.Deserialize<FocusStateOptions>(json);

        Assert.NotNull(options);
        Assert.Single(options!.BlackoutPeriods);
        var bp = options.BlackoutPeriods[0];
        Assert.Equal("Test", bp.Label);
        Assert.Equal("DeepWork", bp.TargetState);
        Assert.Equal("UTC", bp.TimeZoneId);
        Assert.Empty(bp.DaysOfWeek);
    }
}
