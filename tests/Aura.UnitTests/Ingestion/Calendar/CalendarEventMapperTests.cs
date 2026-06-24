using Aura.Domain.Calendar;
using Aura.Infrastructure.Adapters.Connectors.Calendar;

namespace Aura.UnitTests.Ingestion.Calendar;

public class CalendarEventMapperTests
{
    [Fact]
    public void TryMap_ValidDto_ReturnsTrueAndMapsAllFields()
    {
        var mapper = new CalendarEventMapper();
        var dto = new CalendarEventDto
        {
            ExternalId = "ext-1",
            Subject = "Team meeting",
            Start = new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero),
            End = new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero),
            IsOnlineMeeting = true,
            JoinUrl = "https://teams.microsoft.com/l/meetup-join/123",
            OrganizerName = "John Doe",
            OrganizerAddress = "john@example.com",
            LocationDisplayName = "Conference Room A",
            IsCancelled = false,
            OriginalTimeZone = "Eastern Standard Time"
        };

        var result = mapper.TryMap(dto, out var calendarEvent);

        Assert.True(result);
        Assert.NotNull(calendarEvent);
        Assert.Equal("ext-1", calendarEvent.Id);
        Assert.Equal("Team meeting", calendarEvent.Title);
        Assert.Equal(dto.Start.Value.UtcDateTime, calendarEvent.StartUtc.UtcDateTime);
        Assert.Equal(dto.End.Value.UtcDateTime, calendarEvent.EndUtc.UtcDateTime);
        Assert.True(calendarEvent.IsOnlineMeeting);
        Assert.Equal("https://teams.microsoft.com/l/meetup-join/123", calendarEvent.JoinUrl);
        Assert.Equal("John Doe", calendarEvent.Organizer);
        Assert.Equal("Conference Room A", calendarEvent.Location);
        Assert.Equal("Eastern Standard Time", calendarEvent.OriginalTimeZone);
    }

    [Fact]
    public void TryMap_NullOptionalFields_ReturnsTrueAndMapsNulls()
    {
        var mapper = new CalendarEventMapper();
        var dto = new CalendarEventDto
        {
            ExternalId = "ext-2",
            Subject = "All-day event",
            Start = new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero),
            End = new DateTimeOffset(2026, 6, 25, 0, 0, 0, TimeSpan.Zero),
            IsOnlineMeeting = false
        };

        var result = mapper.TryMap(dto, out var calendarEvent);

        Assert.True(result);
        Assert.NotNull(calendarEvent);
        Assert.Equal("ext-2", calendarEvent.Id);
        Assert.Equal("All-day event", calendarEvent.Title);
        Assert.False(calendarEvent.IsOnlineMeeting);
        Assert.Null(calendarEvent.JoinUrl);
        Assert.Null(calendarEvent.Organizer);
        Assert.Null(calendarEvent.Location);
        Assert.Null(calendarEvent.OriginalTimeZone);
    }

    [Fact]
    public void TryMap_CancelledEvent_ReturnsFalse()
    {
        var mapper = new CalendarEventMapper();
        var dto = new CalendarEventDto
        {
            ExternalId = "ext-3",
            Subject = "Cancelled meeting",
            Start = new DateTimeOffset(2026, 6, 24, 14, 0, 0, TimeSpan.Zero),
            End = new DateTimeOffset(2026, 6, 24, 15, 0, 0, TimeSpan.Zero),
            IsOnlineMeeting = false,
            IsCancelled = true
        };

        var result = mapper.TryMap(dto, out var calendarEvent);

        Assert.False(result);
        Assert.Null(calendarEvent);
    }

    [Fact]
    public void TryMap_MissingExternalId_ReturnsFalse()
    {
        var mapper = new CalendarEventMapper();
        var dto = new CalendarEventDto
        {
            ExternalId = null,
            Subject = "Missing ID",
            Start = new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero),
            End = new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero),
            IsOnlineMeeting = false
        };

        var result = mapper.TryMap(dto, out var calendarEvent);

        Assert.False(result);
        Assert.Null(calendarEvent);
    }

    [Fact]
    public void TryMap_MissingSubject_ReturnsFalse()
    {
        var mapper = new CalendarEventMapper();
        var dto = new CalendarEventDto
        {
            ExternalId = "ext-4",
            Subject = null,
            Start = new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero),
            End = new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero),
            IsOnlineMeeting = false
        };

        var result = mapper.TryMap(dto, out var calendarEvent);

        Assert.False(result);
        Assert.Null(calendarEvent);
    }

    [Fact]
    public void TryMap_LocalTimeNormalizedToUtc()
    {
        var mapper = new CalendarEventMapper();
        var localTime = new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.FromHours(-5)); // EST
        var dto = new CalendarEventDto
        {
            ExternalId = "ext-5",
            Subject = "Local time event",
            Start = localTime,
            End = localTime.AddHours(1),
            IsOnlineMeeting = false,
            OriginalTimeZone = "Eastern Standard Time"
        };

        var result = mapper.TryMap(dto, out var calendarEvent);

        Assert.True(result);
        Assert.NotNull(calendarEvent);
        Assert.Equal(localTime.UtcDateTime, calendarEvent.StartUtc.UtcDateTime);
        Assert.Equal(localTime.AddHours(1).UtcDateTime, calendarEvent.EndUtc.UtcDateTime);
        Assert.Equal("Eastern Standard Time", calendarEvent.OriginalTimeZone);
    }
}