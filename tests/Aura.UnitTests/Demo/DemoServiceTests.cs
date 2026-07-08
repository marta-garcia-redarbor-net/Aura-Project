using Aura.Application.Demo;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Aura.Domain.WorkItems;
using NSubstitute;
using WorkItemPersistenceResult = Aura.Application.Models.WorkItemPersistenceResult;

namespace Aura.UnitTests.Demo;

public class DemoServiceTests
{
    private readonly IWorkItemStore _workItemStore = Substitute.For<IWorkItemStore>();
    private readonly IMeetingAlertStore _meetingAlertStore = Substitute.For<IMeetingAlertStore>();
    private readonly IMorningSummaryEmissionStore _morningSummaryEmissionStore = Substitute.For<IMorningSummaryEmissionStore>();
    private readonly INotificationOutboxStore _notificationOutboxStore = Substitute.For<INotificationOutboxStore>();
    private readonly ICalendarEventStore _calendarEventStore = Substitute.For<ICalendarEventStore>();
    private readonly IDashboardRefreshDispatcher _dashboardRefreshDispatcher = Substitute.For<IDashboardRefreshDispatcher>();

    private DemoService CreateSut() => new(
        _workItemStore,
        _meetingAlertStore,
        _morningSummaryEmissionStore,
        _notificationOutboxStore,
        _calendarEventStore,
        _dashboardRefreshDispatcher);

    [Fact]
    public async Task LoadMorningSummaryAsync_MarksEmittedForToday()
    {
        var sut = CreateSut();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await sut.LoadMorningSummaryAsync("demo-user", CancellationToken.None);

        Assert.True(result.Contains("Morning Summary"));
        await _morningSummaryEmissionStore
            .Received(1)
            .MarkEmittedAsync("demo-user", today, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadEmailsAsync_SavesWorkItemsWithOutlookEmailSourceType()
    {
        _workItemStore.SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(WorkItemPersistenceResult.Success());

        var sut = CreateSut();

        var result = await sut.LoadEmailsAsync(CancellationToken.None);

        Assert.True(result.Contains("email"));
        await _workItemStore
            .Received(3)
            .SaveAsync(
                Arg.Is<WorkItem>(w => w.SourceType == WorkItemSourceType.OutlookEmail),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadTeamsMessagesAsync_SavesWorkItemsWithTeamsSourceType()
    {
        _workItemStore.SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(WorkItemPersistenceResult.Success());

        var sut = CreateSut();

        var result = await sut.LoadTeamsMessagesAsync(CancellationToken.None);

        Assert.True(result.Contains("Teams"));
        await _workItemStore
            .Received(3)
            .SaveAsync(
                Arg.Is<WorkItem>(w => w.SourceType == WorkItemSourceType.TeamsMessage),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadCalendarEventsAsync_CreatesMeetingAlerts()
    {
        var sut = CreateSut();

        var result = await sut.LoadCalendarEventsAsync(CancellationToken.None);

        Assert.True(result.Contains("calendar"));
        // Verify meeting alerts were queried (upcoming alerts check)
        await _meetingAlertStore
            .Received(1)
            .GetUpcomingAlertsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadPriorityAlertsAsync_SavesHighPriorityWorkItemsAndNotifications()
    {
        _workItemStore.SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(WorkItemPersistenceResult.Success());

        var sut = CreateSut();

        var result = await sut.LoadPriorityAlertsAsync(CancellationToken.None);

        Assert.True(result.Contains("priority"));
        await _workItemStore
            .Received(2)
            .SaveAsync(
                Arg.Is<WorkItem>(w => w.Priority == WorkItemPriority.Critical || w.Priority == WorkItemPriority.High),
                Arg.Any<CancellationToken>());
        await _notificationOutboxStore
            .Received(2)
            .EnqueueAsync(Arg.Any<NotificationOutboxEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadPullRequestsAsync_SavesWorkItemsWithPrReviewSourceType()
    {
        _workItemStore.SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(WorkItemPersistenceResult.Success());

        var sut = CreateSut();

        var result = await sut.LoadPullRequestsAsync(CancellationToken.None);

        Assert.True(result.Contains("pull request"));
        await _workItemStore
            .Received(2)
            .SaveAsync(
                Arg.Is<WorkItem>(w => w.SourceType == WorkItemSourceType.PrReview),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadAllAsync_CallsAllLoadMethods()
    {
        _workItemStore.SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(WorkItemPersistenceResult.Success());

        var sut = CreateSut();

        var result = await sut.LoadAllAsync("demo-user", CancellationToken.None);

        Assert.True(result.Contains("complete"));
        // Verify all stores were called
        await _morningSummaryEmissionStore
            .Received(1)
            .MarkEmittedAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
        // At least 10 work items saved (3 emails + 3 teams + 2 priority + 2 PRs)
        await _workItemStore
            .Received(10)
            .SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>());
    }
}
