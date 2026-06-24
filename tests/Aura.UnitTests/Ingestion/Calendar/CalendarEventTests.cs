using Aura.Domain.Calendar;

namespace Aura.UnitTests.Ingestion.Calendar;

public class CalendarEventTests
{
    [Fact]
    public void CalendarEvent_CreatesWithAllFields()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddHours(1);
        var calendarEvent = new CalendarEvent(
            Id: "event-1",
            Title: "Team standup",
            StartUtc: start,
            EndUtc: end,
            IsOnlineMeeting: true,
            JoinUrl: "https://teams.microsoft.com/l/meetup-join/123",
            Organizer: "john@example.com",
            Location: "Conference Room A",
            OriginalTimeZone: "Eastern Standard Time");

        Assert.Equal("event-1", calendarEvent.Id);
        Assert.Equal("Team standup", calendarEvent.Title);
        Assert.Equal(start, calendarEvent.StartUtc);
        Assert.Equal(end, calendarEvent.EndUtc);
        Assert.True(calendarEvent.IsOnlineMeeting);
        Assert.Equal("https://teams.microsoft.com/l/meetup-join/123", calendarEvent.JoinUrl);
        Assert.Equal("john@example.com", calendarEvent.Organizer);
        Assert.Equal("Conference Room A", calendarEvent.Location);
        Assert.Equal("Eastern Standard Time", calendarEvent.OriginalTimeZone);
    }

    [Fact]
    public void CalendarEvent_CreatesWithNullableFieldsNull()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddHours(1);
        var calendarEvent = new CalendarEvent(
            Id: "event-2",
            Title: "All-day event",
            StartUtc: start,
            EndUtc: end,
            IsOnlineMeeting: false);

        Assert.Equal("event-2", calendarEvent.Id);
        Assert.Equal("All-day event", calendarEvent.Title);
        Assert.Equal(start, calendarEvent.StartUtc);
        Assert.Equal(end, calendarEvent.EndUtc);
        Assert.False(calendarEvent.IsOnlineMeeting);
        Assert.Null(calendarEvent.JoinUrl);
        Assert.Null(calendarEvent.Organizer);
        Assert.Null(calendarEvent.Location);
        Assert.Null(calendarEvent.OriginalTimeZone);
    }
}