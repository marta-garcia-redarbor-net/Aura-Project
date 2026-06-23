using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.MorningSummaryScheduling;
using NSubstitute;
using System.Globalization;

namespace Aura.UnitTests.MorningSummary;

public class MorningSummarySchedulerTests
{
    [Fact]
    public async Task ResolveAsync_UsesConfiguredTimezone_WhenValid()
    {
        var settingsProvider = Substitute.For<IMorningSummarySettingsProvider>();
        settingsProvider.GetSettings().Returns(new MorningSummarySettings("UTC", new TimeOnly(9, 0)));

        var emissionStore = Substitute.For<IMorningSummaryEmissionStore>();
        emissionStore.HasBeenEmittedAsync("system", new DateOnly(2026, 6, 23), Arg.Any<CancellationToken>())
            .Returns(false);

        var scheduler = new MorningSummaryScheduler(
            settingsProvider,
            emissionStore,
            utcNow: () => DateTimeOffset.Parse("2026-06-23T09:15:00Z", CultureInfo.InvariantCulture));

        var due = await scheduler.ResolveAsync("system", CancellationToken.None);

        Assert.True(due.IsDue);
        Assert.Equal("UTC", due.ResolvedTimezoneId);
        Assert.Equal(new DateOnly(2026, 6, 23), due.LocalDate);
        Assert.Equal(new TimeOnly(9, 0), due.TargetLocalTime);
    }

    [Fact]
    public async Task ResolveAsync_InvalidConfiguredTimezone_FallsBackToSystemTimezone()
    {
        var settingsProvider = Substitute.For<IMorningSummarySettingsProvider>();
        settingsProvider.GetSettings().Returns(new MorningSummarySettings("Invalid/Zone", new TimeOnly(9, 0)));

        var emissionStore = Substitute.For<IMorningSummaryEmissionStore>();
        emissionStore.HasBeenEmittedAsync("system", new DateOnly(2026, 6, 23), Arg.Any<CancellationToken>())
            .Returns(false);

        var scheduler = new MorningSummaryScheduler(
            settingsProvider,
            emissionStore,
            utcNow: () => DateTimeOffset.Parse("2026-06-23T09:15:00Z", CultureInfo.InvariantCulture),
            systemTimeZoneResolver: () => TimeZoneInfo.Utc);

        var due = await scheduler.ResolveAsync("system", CancellationToken.None);

        Assert.Equal(TimeZoneInfo.Utc.Id, due.ResolvedTimezoneId);
        Assert.True(due.IsDue);
    }

    [Fact]
    public async Task ResolveAsync_InvalidConfiguredAndSystemTimezone_FallsBackToUtc()
    {
        var settingsProvider = Substitute.For<IMorningSummarySettingsProvider>();
        settingsProvider.GetSettings().Returns(new MorningSummarySettings("Invalid/Zone", new TimeOnly(9, 0)));

        var emissionStore = Substitute.For<IMorningSummaryEmissionStore>();
        emissionStore.HasBeenEmittedAsync("system", new DateOnly(2026, 6, 23), Arg.Any<CancellationToken>())
            .Returns(false);

        var scheduler = new MorningSummaryScheduler(
            settingsProvider,
            emissionStore,
            utcNow: () => DateTimeOffset.Parse("2026-06-23T09:15:00Z", CultureInfo.InvariantCulture),
            systemTimeZoneResolver: () => throw new InvalidTimeZoneException("No system timezone"));

        var due = await scheduler.ResolveAsync("system", CancellationToken.None);

        Assert.Equal(TimeZoneInfo.Utc.Id, due.ResolvedTimezoneId);
        Assert.True(due.IsDue);
    }

    [Fact]
    public async Task ResolveAsync_UsesDstAwareWallClockComparison()
    {
        var settingsProvider = Substitute.For<IMorningSummarySettingsProvider>();
        settingsProvider.GetSettings().Returns(new MorningSummarySettings("Europe/Madrid", new TimeOnly(9, 0)));

        var emissionStore = Substitute.For<IMorningSummaryEmissionStore>();
        emissionStore.HasBeenEmittedAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var winterScheduler = new MorningSummaryScheduler(
            settingsProvider,
            emissionStore,
            utcNow: () => DateTimeOffset.Parse("2026-01-15T08:30:00Z", CultureInfo.InvariantCulture));

        var summerScheduler = new MorningSummaryScheduler(
            settingsProvider,
            emissionStore,
            utcNow: () => DateTimeOffset.Parse("2026-07-15T07:30:00Z", CultureInfo.InvariantCulture));

        var winterDue = await winterScheduler.ResolveAsync("system", CancellationToken.None);
        var summerDue = await summerScheduler.ResolveAsync("system", CancellationToken.None);

        Assert.True(winterDue.IsDue);
        Assert.True(summerDue.IsDue);
        Assert.Equal("Europe/Madrid", winterDue.ResolvedTimezoneId);
        Assert.Equal("Europe/Madrid", summerDue.ResolvedTimezoneId);
    }

    [Fact]
    public async Task ResolveAsync_BeforeTargetTime_ReturnsNotDue()
    {
        var settingsProvider = Substitute.For<IMorningSummarySettingsProvider>();
        settingsProvider.GetSettings().Returns(new MorningSummarySettings("UTC", new TimeOnly(9, 0)));

        var emissionStore = Substitute.For<IMorningSummaryEmissionStore>();
        emissionStore.HasBeenEmittedAsync("system", new DateOnly(2026, 6, 23), Arg.Any<CancellationToken>())
            .Returns(false);

        var scheduler = new MorningSummaryScheduler(
            settingsProvider,
            emissionStore,
            utcNow: () => DateTimeOffset.Parse("2026-06-23T08:59:59Z", CultureInfo.InvariantCulture));

        var due = await scheduler.ResolveAsync("system", CancellationToken.None);

        Assert.False(due.IsDue);
        Assert.Equal(new DateOnly(2026, 6, 23), due.LocalDate);
    }

    [Fact]
    public async Task ResolveAsync_AlreadyEmittedSameDay_ReturnsNotDue()
    {
        var settingsProvider = Substitute.For<IMorningSummarySettingsProvider>();
        settingsProvider.GetSettings().Returns(new MorningSummarySettings("UTC", new TimeOnly(9, 0)));

        var emissionStore = Substitute.For<IMorningSummaryEmissionStore>();
        emissionStore.HasBeenEmittedAsync("system", new DateOnly(2026, 6, 23), Arg.Any<CancellationToken>())
            .Returns(true);

        var scheduler = new MorningSummaryScheduler(
            settingsProvider,
            emissionStore,
            utcNow: () => DateTimeOffset.Parse("2026-06-23T10:00:00Z", CultureInfo.InvariantCulture));

        var due = await scheduler.ResolveAsync("system", CancellationToken.None);

        Assert.False(due.IsDue);
    }
}
