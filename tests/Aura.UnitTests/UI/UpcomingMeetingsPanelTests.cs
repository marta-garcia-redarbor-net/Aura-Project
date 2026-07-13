using Aura.Application.Ports;
using Aura.Application.UseCases.Calendar;
using Aura.Application.Models;
using Aura.Domain.Calendar;
using Aura.UI.Components.Dashboard;
using Aura.UI.Services;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Security.Claims;

namespace Aura.UnitTests.UI;

public class UpcomingMeetingsPanelTests : TestContext
{
    private const string UserId = "user-1";

    public UpcomingMeetingsPanelTests()
    {
        Services.AddHttpContextAccessor();
        Services.AddCascadingAuthenticationState();
        Services.AddSingleton<IDashboardEventBus>(new DashboardEventBus());
        
        // Register a mock AuthenticationStateProvider
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", UserId)], "TestAuth"));
        authStateProvider.GetAuthenticationStateAsync()
            .Returns(Task.FromResult(new AuthenticationState(principal)));
        Services.AddSingleton(authStateProvider);
    }

    private void SetAuthenticatedUser(string userId = UserId)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim("oid", userId)
            ],
            authenticationType: "TestAuth"));

        var context = new DefaultHttpContext { User = principal };
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = context });
    }

    [Fact]
    public void Renders_LoadingState_Initially()
    {
        SetAuthenticatedUser();
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.Delay(5000).ContinueWith(_ => (IReadOnlyList<CalendarEvent>)Array.Empty<CalendarEvent>()));

        var useCase = new GetUpcomingMeetingsUseCase(store);
        Services.AddSingleton(useCase);

        var cut = RenderComponent<UpcomingMeetingsPanel>();

        cut.Find("[data-testid='upcoming-meetings-loading']");
    }

    [Fact]
    public async Task Renders_PopulatedState_WhenEventsExist()
    {
        SetAuthenticatedUser();
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
        store.GetUpcomingAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
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
        SetAuthenticatedUser();
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
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
        SetAuthenticatedUser();
        var store = Substitute.For<ICalendarEventStore>();
        store.GetUpcomingAsync(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<CalendarEvent>>(new InvalidOperationException("Database error")));

        var useCase = new GetUpcomingMeetingsUseCase(store);
        Services.AddSingleton(useCase);

        var cut = RenderComponent<UpcomingMeetingsPanel>();

        await cut.InvokeAsync(() => Task.CompletedTask);

        Assert.NotNull(cut.Find("[data-testid='upcoming-meetings-error']"));
    }
}
