using Aura.Application.Ports;
using Aura.Application.UseCases.Calendar;
using Aura.Domain.Calendar;
using NSubstitute;

namespace Aura.UnitTests.UseCases.Calendar;

public class CheckAndDispatchMeetingAlertsUseCaseTests
{
    private readonly ICalendarEventStore _eventStore = Substitute.For<ICalendarEventStore>();
    private readonly IMeetingAlertStore _alertStore = Substitute.For<IMeetingAlertStore>();
    private readonly IMeetingAlertDispatcher _dispatcher = Substitute.For<IMeetingAlertDispatcher>();

    private readonly CheckAndDispatchMeetingAlertsUseCase _sut;

    public CheckAndDispatchMeetingAlertsUseCaseTests()
    {
        _sut = new CheckAndDispatchMeetingAlertsUseCase(_eventStore, _alertStore, _dispatcher);
    }

    [Fact]
    public async Task ExecuteAsync_EventStartingIn60Minutes_DispatchesSixtyMinuteAlert()
    {
        // Arrange — event starts exactly 60 minutes from now
        var now = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero);
        var eventStart = now.AddMinutes(60);
        var calendarEvent = CreateEvent("evt-1", "Team standup", eventStart);

        _eventStore.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new[] { calendarEvent });

        _alertStore.GetUnsentAlertAsync("evt-1", MeetingAlertTrigger.SixtyMinutes, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((MeetingAlert?)null);

        // Act
        await _sut.ExecuteAsync(now, CancellationToken.None);

        // Assert
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<MeetingAlert>(a =>
                a.EventId == "evt-1" &&
                a.Trigger == MeetingAlertTrigger.SixtyMinutes &&
                a.Title == "Team standup"),
            Arg.Any<CancellationToken>());

        await _alertStore.Received(1).MarkSentAsync(
            Arg.Is<MeetingAlert>(a =>
                a.EventId == "evt-1" &&
                a.Trigger == MeetingAlertTrigger.SixtyMinutes),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_AllAlertsAlreadySent_DoesNotDispatch()
    {
        // Arrange — all three alerts already sent for this event
        var now = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero);
        var eventStart = now.AddMinutes(60); // 13:00 — triggers at 12:00, 12:50, 12:55
        var calendarEvent = CreateEvent("evt-1", "Sprint planning", eventStart);

        _eventStore.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new[] { calendarEvent });

        // All three triggers already sent
        foreach (var trigger in Enum.GetValues<MeetingAlertTrigger>())
        {
            var existingAlert = new MeetingAlert("evt-1", "Sprint planning", trigger, eventStart, null, "user-1", HasBeenSent: true);
            _alertStore.GetUnsentAlertAsync("evt-1", trigger, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
                .Returns(existingAlert);
        }

        // Act
        await _sut.ExecuteAsync(now, CancellationToken.None);

        // Assert — dispatcher NOT called
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<MeetingAlert>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_MultipleEvents_DispatchesForEach()
    {
        // Arrange — two events, different trigger windows
        var now = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero);
        var event1 = CreateEvent("evt-1", "Standup", now.AddMinutes(60));
        var event2 = CreateEvent("evt-2", "Review", now.AddMinutes(10));

        _eventStore.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new[] { event1, event2 });

        _alertStore.GetUnsentAlertAsync("evt-1", MeetingAlertTrigger.SixtyMinutes, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((MeetingAlert?)null);
        _alertStore.GetUnsentAlertAsync("evt-2", MeetingAlertTrigger.TenMinutes, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((MeetingAlert?)null);

        // Act
        await _sut.ExecuteAsync(now, CancellationToken.None);

        // Assert — dispatcher called twice
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<MeetingAlert>(a => a.EventId == "evt-1" && a.Trigger == MeetingAlertTrigger.SixtyMinutes),
            Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).DispatchAsync(
            Arg.Is<MeetingAlert>(a => a.EventId == "evt-2" && a.Trigger == MeetingAlertTrigger.TenMinutes),
            Arg.Any<CancellationToken>());
    }

    private static CalendarEvent CreateEvent(string id, string title, DateTimeOffset startUtc) =>
        new(id, title, startUtc, startUtc.AddMinutes(30), IsOnlineMeeting: true, JoinUrl: "https://teams.live.com/abc");
}
