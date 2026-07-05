using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Aura.Domain.FocusState;
using Aura.Infrastructure.Adapters.Options;
using Aura.Infrastructure.Adapters.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aura.UnitTests.Services;

public sealed class SignalBasedFocusStateResolverTests
{
    private const string TestUserId = "user-1";

    private static SignalBasedFocusStateResolver CreateResolver(
        ICalendarEventStore? store = null,
        FocusStateOptions? options = null,
        DateTimeOffset? fixedTime = null)
    {
        store ??= CreateEmptyStore();
        options ??= new FocusStateOptions();
        var timeProvider = new FakeTimeProvider(fixedTime ?? new DateTimeOffset(2026, 7, 8, 11, 0, 0, TimeSpan.Zero));
        return new SignalBasedFocusStateResolver(store, Options.Create(options), timeProvider);
    }

    private static ICalendarEventStore CreateEmptyStore()
    {
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CalendarEvent>());
        return store;
    }

    private static CalendarEvent CreateEvent(string userId, DateTimeOffset start, DateTimeOffset end)
        => new("evt-1", "Meeting", start, end, false, UserId: userId);

    // ============================================================
    // Signal 1: Calendar meeting → Away
    // ============================================================

    [Fact]
    public async Task ResolveAsync_ActiveCalendarEvent_ReturnsAway()
    {
        var now = new DateTimeOffset(2026, 7, 8, 11, 0, 0, TimeSpan.Zero);
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEvent(TestUserId, now.AddMinutes(-30), now.AddMinutes(30)) });

        var resolver = CreateResolver(store, fixedTime: now);
        var result = await resolver.ResolveAsync(TestUserId);

        Assert.Equal(FocusStateType.Away, result.CurrentState);
    }

    [Fact]
    public async Task ResolveAsync_CalendarEvent_OtherUserId_ReturnsWindowOfOpportunity()
    {
        var now = new DateTimeOffset(2026, 7, 8, 11, 0, 0, TimeSpan.Zero);
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEvent("other-user", now.AddMinutes(-30), now.AddMinutes(30)) });

        var resolver = CreateResolver(store, fixedTime: now);
        var result = await resolver.ResolveAsync(TestUserId);

        Assert.Equal(FocusStateType.WindowOfOpportunity, result.CurrentState);
    }

    [Fact]
    public async Task ResolveAsync_CalendarEvent_OtherUserId_LogsNoCalendarStoreMatchWarning()
    {
        var now = new DateTimeOffset(2026, 7, 8, 11, 0, 0, TimeSpan.Zero);
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEvent("other-user", now.AddMinutes(-30), now.AddMinutes(30)) });

        var logger = new RecordingLogger<SignalBasedFocusStateResolver>();
        var resolver = new SignalBasedFocusStateResolver(
            store,
            Options.Create(new FocusStateOptions()),
            new FakeTimeProvider(now),
            logger);

        var result = await resolver.ResolveAsync(TestUserId);

        Assert.Equal(FocusStateType.WindowOfOpportunity, result.CurrentState);
        Assert.Contains(logger.Entries, e =>
            e.Level == LogLevel.Warning &&
            e.EventId.Id == 4804 &&
            e.Message.Contains(TestUserId, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResolveAsync_CalendarEvent_NullUserId_MatchesAnyUser()
    {
        var now = new DateTimeOffset(2026, 7, 8, 11, 0, 0, TimeSpan.Zero);
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEvent(null!, now.AddMinutes(-30), now.AddMinutes(30)) });

        var resolver = CreateResolver(store, fixedTime: now);
        var result = await resolver.ResolveAsync(TestUserId);

        Assert.Equal(FocusStateType.Away, result.CurrentState);
    }

    // ============================================================
    // Calendar overrides blackout (Signal priority)
    // ============================================================

    [Fact]
    public async Task ResolveAsync_CalendarOverridesBlackout_ReturnsAway()
    {
        var now = new DateTimeOffset(2026, 7, 8, 11, 0, 0, TimeSpan.Zero);
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new[] { CreateEvent(TestUserId, now.AddMinutes(-5), now.AddMinutes(5)) });

        var options = new FocusStateOptions
        {
            BlackoutPeriods =
            [
                new BlackoutPeriodDto
                {
                    Label = "Deep Work Block",
                    TargetState = "DeepWork",
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0),
                    DaysOfWeek = [DayOfWeek.Wednesday],
                    TimeZoneId = "UTC"
                }
            ]
        };

        var resolver = CreateResolver(store, options, now);
        var result = await resolver.ResolveAsync(TestUserId);

        // Calendar (Away) takes priority over blackout (DeepWork)
        Assert.Equal(FocusStateType.Away, result.CurrentState);
    }

    // ============================================================
    // Signal 2: Blackout → target state
    // ============================================================

    [Fact]
    public async Task ResolveAsync_BlackoutDeepWork_ReturnsDeepWork()
    {
        var now = new DateTimeOffset(2026, 7, 8, 11, 0, 0, TimeSpan.Zero); // Wednesday
        var options = new FocusStateOptions
        {
            BlackoutPeriods =
            [
                new BlackoutPeriodDto
                {
                    Label = "Morning Block",
                    TargetState = "DeepWork",
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0),
                    DaysOfWeek = [DayOfWeek.Wednesday],
                    TimeZoneId = "UTC"
                }
            ]
        };

        var resolver = CreateResolver(options: options, fixedTime: now);
        var result = await resolver.ResolveAsync(TestUserId);

        Assert.Equal(FocusStateType.DeepWork, result.CurrentState);
    }

    [Fact]
    public async Task ResolveAsync_BlackoutAway_ReturnsAway()
    {
        var now = new DateTimeOffset(2026, 7, 8, 12, 30, 0, TimeSpan.Zero); // Wednesday
        var options = new FocusStateOptions
        {
            BlackoutPeriods =
            [
                new BlackoutPeriodDto
                {
                    Label = "Lunch",
                    TargetState = "Away",
                    StartTime = new TimeOnly(12, 0),
                    EndTime = new TimeOnly(13, 0),
                    DaysOfWeek = [DayOfWeek.Wednesday],
                    TimeZoneId = "UTC"
                }
            ]
        };

        var resolver = CreateResolver(options: options, fixedTime: now);
        var result = await resolver.ResolveAsync(TestUserId);

        Assert.Equal(FocusStateType.Away, result.CurrentState);
    }

    // ============================================================
    // Signal 3: Outside working hours → Away
    // ============================================================

    [Fact]
    public async Task ResolveAsync_OutsideWorkingHours_ReturnsAway()
    {
        var now = new DateTimeOffset(2026, 7, 8, 22, 0, 0, TimeSpan.Zero); // 10 PM
        var resolver = CreateResolver(fixedTime: now);
        var result = await resolver.ResolveAsync(TestUserId);

        Assert.Equal(FocusStateType.Away, result.CurrentState);
    }

    // ============================================================
    // Signal 4: Fallback → WindowOfOpportunity
    // ============================================================

    [Fact]
    public async Task ResolveAsync_NoSignalsWithinHours_ReturnsWindowOfOpportunity()
    {
        var now = new DateTimeOffset(2026, 7, 8, 14, 0, 0, TimeSpan.Zero); // 2 PM, Wednesday
        var resolver = CreateResolver(fixedTime: now);
        var result = await resolver.ResolveAsync(TestUserId);

        Assert.Equal(FocusStateType.WindowOfOpportunity, result.CurrentState);
    }

    // ============================================================
    // Edge cases
    // ============================================================

    [Fact]
    public async Task ResolveAsync_CalendarStoreThrows_FallsThroughToBlackout()
    {
        var now = new DateTimeOffset(2026, 7, 8, 11, 0, 0, TimeSpan.Zero);
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Store unavailable"));

        var options = new FocusStateOptions
        {
            BlackoutPeriods =
            [
                new BlackoutPeriodDto
                {
                    Label = "Morning Block",
                    TargetState = "DeepWork",
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0),
                    DaysOfWeek = [DayOfWeek.Wednesday],
                    TimeZoneId = "UTC"
                }
            ]
        };

        var resolver = CreateResolver(store, options, now);
        var result = await resolver.ResolveAsync(TestUserId);

        // Should fall through to blackout despite store failure
        Assert.Equal(FocusStateType.DeepWork, result.CurrentState);
    }

    [Fact]
    public async Task ResolveAsync_BlackoutOnWrongDay_ReturnsFallback()
    {
        var now = new DateTimeOffset(2026, 7, 8, 11, 0, 0, TimeSpan.Zero); // Wednesday
        var options = new FocusStateOptions
        {
            BlackoutPeriods =
            [
                new BlackoutPeriodDto
                {
                    Label = "Monday Only",
                    TargetState = "DeepWork",
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0),
                    DaysOfWeek = [DayOfWeek.Monday],
                    TimeZoneId = "UTC"
                }
            ]
        };

        var resolver = CreateResolver(options: options, fixedTime: now);
        var result = await resolver.ResolveAsync(TestUserId);

        Assert.Equal(FocusStateType.WindowOfOpportunity, result.CurrentState);
    }

    [Fact]
    public async Task ResolveAsync_WithWhitespaceUserId_ThrowsArgumentException()
    {
        var resolver = CreateResolver();

        await Assert.ThrowsAsync<ArgumentException>(() => resolver.ResolveAsync("   "));
    }

    /// <summary>
    /// Fixed <see cref="TimeProvider"/> for deterministic test timing.
    /// </summary>
    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _fixedTime;

        public FakeTimeProvider(DateTimeOffset fixedTime)
        {
            _fixedTime = fixedTime;
        }

        public override DateTimeOffset GetUtcNow() => _fixedTime;
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public IList<LogEntry> Entries { get; } = new List<LogEntry>();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
            => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    private sealed record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);
}
