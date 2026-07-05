using Aura.Domain.Calendar;
using Aura.Infrastructure.Adapters.Connectors.Calendar;

namespace Aura.UnitTests.Ingestion.Calendar;

public class InMemoryCalendarEventStoreTests
{
    private const string UserId = "user-1";

    [Fact]
    public async Task SaveAsync_AddsEventToStore()
    {
        var store = new InMemoryCalendarEventStore();
        var calendarEvent = new CalendarEvent(
            Id: "event-1",
            Title: "Team standup",
            StartUtc: new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero),
            EndUtc: new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero),
            IsOnlineMeeting: true,
            UserId: UserId);

        await store.SaveAsync(calendarEvent, CancellationToken.None);

        var upcoming = await store.GetUpcomingAsync(
            UserId,
            new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 24, 23, 59, 59, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Single(upcoming);
        Assert.Equal("event-1", upcoming[0].Id);
    }

    [Fact]
    public async Task SaveAsync_UpsertsSameId()
    {
        var store = new InMemoryCalendarEventStore();
        var event1 = new CalendarEvent(
            Id: "event-1",
            Title: "Original",
            StartUtc: new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero),
            EndUtc: new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero),
            IsOnlineMeeting: false,
            UserId: UserId);
        var event2 = new CalendarEvent(
            Id: "event-1",
            Title: "Updated",
            StartUtc: new DateTimeOffset(2026, 6, 24, 14, 0, 0, TimeSpan.Zero),
            EndUtc: new DateTimeOffset(2026, 6, 24, 15, 0, 0, TimeSpan.Zero),
            IsOnlineMeeting: true,
            UserId: UserId);

        await store.SaveAsync(event1, CancellationToken.None);
        await store.SaveAsync(event2, CancellationToken.None);

        var upcoming = await store.GetUpcomingAsync(
            UserId,
            new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 24, 23, 59, 59, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Single(upcoming);
        Assert.Equal("Updated", upcoming[0].Title);
        Assert.True(upcoming[0].IsOnlineMeeting);
    }

    [Fact]
    public async Task SaveBatchAsync_AddsAllEvents()
    {
        var store = new InMemoryCalendarEventStore();
        var events = new[]
        {
            new CalendarEvent(
                Id: "event-1",
                Title: "Event 1",
                StartUtc: new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero),
                EndUtc: new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting: false,
                UserId: UserId),
            new CalendarEvent(
                Id: "event-2",
                Title: "Event 2",
                StartUtc: new DateTimeOffset(2026, 6, 24, 14, 0, 0, TimeSpan.Zero),
                EndUtc: new DateTimeOffset(2026, 6, 24, 15, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting: true,
                UserId: UserId)
        };

        await store.SaveBatchAsync(events, CancellationToken.None);

        var upcoming = await store.GetUpcomingAsync(
            UserId,
            new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 24, 23, 59, 59, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Equal(2, upcoming.Count);
    }

    [Fact]
    public async Task GetUpcomingAsync_ReturnsEventsWithinTimeWindow()
    {
        var store = new InMemoryCalendarEventStore();
        var insideEvent = new CalendarEvent(
            Id: "inside",
            Title: "Inside window",
            StartUtc: new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero),
            EndUtc: new DateTimeOffset(2026, 6, 24, 13, 0, 0, TimeSpan.Zero),
            IsOnlineMeeting: false,
            UserId: UserId);
        var outsideEvent = new CalendarEvent(
            Id: "outside",
            Title: "Outside window",
            StartUtc: new DateTimeOffset(2026, 6, 25, 10, 0, 0, TimeSpan.Zero),
            EndUtc: new DateTimeOffset(2026, 6, 25, 11, 0, 0, TimeSpan.Zero),
            IsOnlineMeeting: false,
            UserId: UserId);

        await store.SaveAsync(insideEvent, CancellationToken.None);
        await store.SaveAsync(outsideEvent, CancellationToken.None);

        var upcoming = await store.GetUpcomingAsync(
            UserId,
            new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 24, 23, 59, 59, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Single(upcoming);
        Assert.Equal("inside", upcoming[0].Id);
    }

    [Fact]
    public async Task GetUpcomingAsync_ReturnsEmptyWhenNoEvents()
    {
        var store = new InMemoryCalendarEventStore();

        var upcoming = await store.GetUpcomingAsync(
            UserId,
            new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 6, 24, 23, 59, 59, TimeSpan.Zero),
            CancellationToken.None);

        Assert.Empty(upcoming);
    }

    [Fact]
    public async Task GetUpcomingAsync_OverlappingEventsFromDifferentUsers_ReturnsOnlyRequestedUserEvents()
    {
        var store = new InMemoryCalendarEventStore();
        var from = new DateTimeOffset(2026, 6, 24, 9, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero);

        await store.SaveBatchAsync(
        [
            new CalendarEvent(
                Id: "event-user-a",
                Title: "User A meeting",
                StartUtc: new DateTimeOffset(2026, 6, 24, 9, 30, 0, TimeSpan.Zero),
                EndUtc: new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero),
                IsOnlineMeeting: true,
                UserId: "user-a"),
            new CalendarEvent(
                Id: "event-user-b",
                Title: "User B meeting",
                StartUtc: new DateTimeOffset(2026, 6, 24, 9, 45, 0, TimeSpan.Zero),
                EndUtc: new DateTimeOffset(2026, 6, 24, 10, 15, 0, TimeSpan.Zero),
                IsOnlineMeeting: true,
                UserId: "user-b")
        ], CancellationToken.None);

        var upcoming = await store.GetUpcomingAsync("user-a", from, to, CancellationToken.None);

        Assert.Single(upcoming);
        Assert.Contains(upcoming, e => e.UserId == "user-a");
    }
}
