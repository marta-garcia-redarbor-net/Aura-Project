using Aura.Application.Ports;
using Aura.Application.UseCases.Calendar;
using Aura.Application.Models;
using Aura.Domain.Calendar;
using NSubstitute;

namespace Aura.UnitTests.UseCases.Calendar;

public class GetUpcomingMeetingsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsEventsSortedByStartUtcAscending()
    {
        var store = Substitute.For<ICalendarEventStore>();
        var events = new List<CalendarEvent>
        {
            new CalendarEvent("2", "Later meeting", new DateTimeOffset(2026, 6, 24, 14, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 24, 15, 0, 0, TimeSpan.Zero), false),
            new CalendarEvent("1", "Earlier meeting", new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero), false),
            new CalendarEvent("3", "Latest meeting", new DateTimeOffset(2026, 6, 24, 16, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 24, 17, 0, 0, TimeSpan.Zero), true)
        };
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CalendarEvent>>(events));

        var useCase = new GetUpcomingMeetingsUseCase(store);
        var from = new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 6, 24, 23, 59, 59, TimeSpan.Zero);

        var result = await useCase.ExecuteAsync(from, to, CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.IsType<UpcomingMeetingDto>(result[0]);
        Assert.Equal("1", result[0].Id);
        Assert.Equal("2", result[1].Id);
        Assert.Equal("3", result[2].Id);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyStore_ReturnsEmptyList()
    {
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CalendarEvent>>(new List<CalendarEvent>()));

        var useCase = new GetUpcomingMeetingsUseCase(store);
        var from = new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 6, 24, 23, 59, 59, TimeSpan.Zero);

        var result = await useCase.ExecuteAsync(from, to, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_CallsStoreWithCorrectTimeWindow()
    {
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CalendarEvent>>(new List<CalendarEvent>()));

        var useCase = new GetUpcomingMeetingsUseCase(store);
        var from = new DateTimeOffset(2026, 6, 24, 8, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 6, 24, 16, 0, 0, TimeSpan.Zero);

        await useCase.ExecuteAsync(from, to, CancellationToken.None);

        await store.Received(1).GetUpcomingAsync(from, to, Arg.Any<CancellationToken>());
    }
}
