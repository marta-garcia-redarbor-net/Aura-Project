using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Workers;
using Microsoft.Extensions.DependencyInjection;
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
        var composer = Substitute.For<IMorningSummaryComposer>();

        var dueState = new MorningSummaryDueState(
            IsDue: true,
            ResolvedTimezoneId: "UTC",
            LocalDate: new DateOnly(2026, 6, 23),
            TargetLocalTime: new TimeOnly(9, 0));

        scheduler.ResolveAsync("system", Arg.Any<CancellationToken>())
            .Returns(dueState);

        var worker = new MorningSummarySchedulingWorker(
            CreateScopeFactory(scheduler, store, composer),
            NullLogger<MorningSummarySchedulingWorker>.Instance);

        await worker.ProcessIterationAsync(CancellationToken.None);

        await scheduler.Received(1).ResolveAsync("system", Arg.Any<CancellationToken>());
        await store.Received(1).MarkEmittedAsync("system", dueState.LocalDate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessIterationAsync_WhenNotDue_DoesNotMarkEmission()
    {
        var scheduler = Substitute.For<IMorningSummaryScheduler>();
        var store = Substitute.For<IMorningSummaryEmissionStore>();
        var composer = Substitute.For<IMorningSummaryComposer>();

        scheduler.ResolveAsync("system", Arg.Any<CancellationToken>())
            .Returns(new MorningSummaryDueState(
                IsDue: false,
                ResolvedTimezoneId: "UTC",
                LocalDate: new DateOnly(2026, 6, 23),
                TargetLocalTime: new TimeOnly(9, 0)));

        var worker = new MorningSummarySchedulingWorker(
            CreateScopeFactory(scheduler, store, composer),
            NullLogger<MorningSummarySchedulingWorker>.Instance);

        await worker.ProcessIterationAsync(CancellationToken.None);

        await store.DidNotReceive().MarkEmittedAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessIterationAsync_WhenDue_ComposerCalledAfterEmission()
    {
        var scheduler = Substitute.For<IMorningSummaryScheduler>();
        var store = Substitute.For<IMorningSummaryEmissionStore>();
        var composer = Substitute.For<IMorningSummaryComposer>();

        var dueState = new MorningSummaryDueState(
            IsDue: true,
            ResolvedTimezoneId: "UTC",
            LocalDate: new DateOnly(2026, 6, 23),
            TargetLocalTime: new TimeOnly(9, 0));

        scheduler.ResolveAsync("system", Arg.Any<CancellationToken>())
            .Returns(dueState);

        var worker = new MorningSummarySchedulingWorker(
            CreateScopeFactory(scheduler, store, composer),
            NullLogger<MorningSummarySchedulingWorker>.Instance);

        await worker.ProcessIterationAsync(CancellationToken.None);

        // Composer should be called after emission is marked
        await composer.Received(1).ComposeAsync(
            Arg.Any<MorningSummaryRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessIterationAsync_WhenNotDue_ComposerNotCalled()
    {
        var scheduler = Substitute.For<IMorningSummaryScheduler>();
        var store = Substitute.For<IMorningSummaryEmissionStore>();
        var composer = Substitute.For<IMorningSummaryComposer>();

        scheduler.ResolveAsync("system", Arg.Any<CancellationToken>())
            .Returns(new MorningSummaryDueState(
                IsDue: false,
                ResolvedTimezoneId: "UTC",
                LocalDate: new DateOnly(2026, 6, 23),
                TargetLocalTime: new TimeOnly(9, 0)));

        var worker = new MorningSummarySchedulingWorker(
            CreateScopeFactory(scheduler, store, composer),
            NullLogger<MorningSummarySchedulingWorker>.Instance);

        await worker.ProcessIterationAsync(CancellationToken.None);

        await composer.DidNotReceive().ComposeAsync(
            Arg.Any<MorningSummaryRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessIterationAsync_ComposerThrows_DoesNotBreakWorker()
    {
        var scheduler = Substitute.For<IMorningSummaryScheduler>();
        var store = Substitute.For<IMorningSummaryEmissionStore>();
        var composer = Substitute.For<IMorningSummaryComposer>();

        var dueState = new MorningSummaryDueState(
            IsDue: true,
            ResolvedTimezoneId: "UTC",
            LocalDate: new DateOnly(2026, 6, 23),
            TargetLocalTime: new TimeOnly(9, 0));

        scheduler.ResolveAsync("system", Arg.Any<CancellationToken>())
            .Returns(dueState);

        composer.ComposeAsync(Arg.Any<MorningSummaryRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Aura.Application.Models.MorningSummary>(new InvalidOperationException("Composition failed")));

        var worker = new MorningSummarySchedulingWorker(
            CreateScopeFactory(scheduler, store, composer),
            NullLogger<MorningSummarySchedulingWorker>.Instance);

        // Should not throw — composition failure is caught
        var exception = await Record.ExceptionAsync(() => worker.ProcessIterationAsync(CancellationToken.None));
        Assert.Null(exception);

        // Emission should still be marked despite composition failure
        await store.Received(1).MarkEmittedAsync("system", dueState.LocalDate, Arg.Any<CancellationToken>());
}

    private static IServiceScopeFactory CreateScopeFactory(
        IMorningSummaryScheduler scheduler,
        IMorningSummaryEmissionStore store,
        IMorningSummaryComposer composer)
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IMorningSummaryScheduler)).Returns(scheduler);
        serviceProvider.GetService(typeof(IMorningSummaryEmissionStore)).Returns(store);
        serviceProvider.GetService(typeof(IMorningSummaryComposer)).Returns(composer);

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(serviceProvider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        return scopeFactory;
    }
}
