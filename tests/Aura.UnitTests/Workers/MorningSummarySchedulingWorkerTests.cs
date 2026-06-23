using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aura.UnitTests.Workers;

public class MorningSummarySchedulingWorkerTests
{
    [Fact]
    public async Task ProcessIterationAsync_WhenDue_MarksEmissionWithFixedSystemUser()
    {
        var scheduler = Substitute.For<IMorningSummaryScheduler>();
        var store = Substitute.For<IMorningSummaryEmissionStore>();

        var dueState = new MorningSummaryDueState(
            IsDue: true,
            ResolvedTimezoneId: "UTC",
            LocalDate: new DateOnly(2026, 6, 23),
            TargetLocalTime: new TimeOnly(9, 0));

        scheduler.ResolveAsync("system", Arg.Any<CancellationToken>())
            .Returns(dueState);

        var worker = new MorningSummarySchedulingWorker(scheduler, store, NullLogger<MorningSummarySchedulingWorker>.Instance);

        await worker.ProcessIterationAsync(CancellationToken.None);

        await scheduler.Received(1).ResolveAsync("system", Arg.Any<CancellationToken>());
        await store.Received(1).MarkEmittedAsync("system", dueState.LocalDate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessIterationAsync_WhenNotDue_DoesNotMarkEmission()
    {
        var scheduler = Substitute.For<IMorningSummaryScheduler>();
        var store = Substitute.For<IMorningSummaryEmissionStore>();

        scheduler.ResolveAsync("system", Arg.Any<CancellationToken>())
            .Returns(new MorningSummaryDueState(
                IsDue: false,
                ResolvedTimezoneId: "UTC",
                LocalDate: new DateOnly(2026, 6, 23),
                TargetLocalTime: new TimeOnly(9, 0)));

        var worker = new MorningSummarySchedulingWorker(scheduler, store, NullLogger<MorningSummarySchedulingWorker>.Instance);

        await worker.ProcessIterationAsync(CancellationToken.None);

        await store.DidNotReceive().MarkEmittedAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }
}
