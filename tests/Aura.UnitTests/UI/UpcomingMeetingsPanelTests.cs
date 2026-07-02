using Aura.Application.Ports;
using Aura.Application.UseCases.Calendar;
using Aura.Application.Models;
using Aura.Domain.Calendar;
using Aura.UI.Components.Dashboard;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.UI;

public class UpcomingMeetingsPanelTests : TestContext
{
    [Fact]
    public void Renders_LoadingState_Initially()
    {
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.Delay(5000).ContinueWith(_ => (IReadOnlyList<CalendarEvent>)Array.Empty<CalendarEvent>()));

        var useCase = new GetUpcomingMeetingsUseCase(store);
        Services.AddSingleton(useCase);

        var cut = RenderComponent<UpcomingMeetingsPanel>();

        cut.Find("[data-testid='upcoming-meetings-loading']");
    }

    [Fact]
    public async Task Renders_PopulatedState_WhenEventsExist()
    {
        var store = Substitute.For<ICalendarEventStore>();
        var events = new List<CalendarEvent>
        {
            new CalendarEvent("1", "Team standup", new DateTimeOffset(2026, 6, 24, 10, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 24, 11, 0, 0, TimeSpan.Zero), true, "https://teams.microsoft.com/l/meetup-join/123")
        };
        var expectedMeeting = new UpcomingMeetingDto(
            events[0].Id,
            events[0].Title,
            events[0].StartUtc,
            events[0].EndUtc,
            events[0].IsOnlineMeeting,
            events[0].JoinUrl,
            events[0].Organizer,
            events[0].Location);
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CalendarEvent>>(events));

        var useCase = new GetUpcomingMeetingsUseCase(store);
        Services.AddSingleton(useCase);

        var cut = RenderComponent<UpcomingMeetingsPanel>();

        await cut.InvokeAsync(() => Task.CompletedTask);

        Assert.NotNull(cut.Find("[data-testid='upcoming-meetings-populated']"));
        Assert.Equal(expectedMeeting.Title, cut.Find("[data-testid='upcoming-meeting-title']").TextContent);
    }

    [Fact]
    public async Task Renders_EmptyState_WhenNoEvents()
    {
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CalendarEvent>>(new List<CalendarEvent>()));

        var useCase = new GetUpcomingMeetingsUseCase(store);
        Services.AddSingleton(useCase);

        var cut = RenderComponent<UpcomingMeetingsPanel>();

        await cut.InvokeAsync(() => Task.CompletedTask);

        Assert.NotNull(cut.Find("[data-testid='upcoming-meetings-empty']"));
    }

    [Fact]
    public async Task Renders_ErrorState_WhenExceptionThrown()
    {
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<CalendarEvent>>(new InvalidOperationException("Database error")));

        var useCase = new GetUpcomingMeetingsUseCase(store);
        Services.AddSingleton(useCase);

        var cut = RenderComponent<UpcomingMeetingsPanel>();

        await cut.InvokeAsync(() => Task.CompletedTask);

        Assert.NotNull(cut.Find("[data-testid='upcoming-meetings-error']"));
    }
}
