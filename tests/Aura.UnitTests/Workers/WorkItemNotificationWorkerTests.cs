using Aura.Api.Workers;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.UnitTests.Workers;

public class WorkItemNotificationWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_PersistedVerdictPath_UsesPersistedDecision()
    {
        var entry = new NotificationOutboxEntry(
            id: Guid.NewGuid(),
            workItemId: Guid.NewGuid(),
            userId: "user-abc",
            sourceType: "TeamsMessage",
            title: "VIP alert",
            priority: 5.0,
            triggerRule: "vip_sender",
            createdAt: DateTimeOffset.UtcNow,
            dispatchedAt: null,
            explanation: "VIP sender",
            decision: "InterruptNow",
            targetUserId: "user-abc",
            ruleResults: "[{\"RuleName\":\"vip_sender\",\"Matched\":true,\"Score\":9.0,\"Confidence\":0.95}]");

        var store = Substitute.For<INotificationOutboxStore>();
        store.GetPendingAsync(10, Arg.Any<CancellationToken>()).Returns(new[] { entry });

        var dispatcher = Substitute.For<IWorkItemNotificationDispatcher>();

        var services = new ServiceCollection();
        services.AddSingleton(store);
        services.AddSingleton(dispatcher);
        await using var provider = services.BuildServiceProvider();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var worker = new WorkItemNotificationWorker(
            scopeFactory,
            Substitute.For<ILogger<WorkItemNotificationWorker>>());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(300);

        await dispatcher.Received(1).DispatchAsync(
            Arg.Is<NotificationOutboxEntry>(e => e.Id == entry.Id),
            Arg.Is<InterruptionVerdict>(v =>
                v.Decision == InterruptionDecision.InterruptNow &&
                v.Explanation == "VIP sender" &&
                v.TargetUserId == "user-abc" &&
                v.TriggerRule == "vip_sender" &&
                v.Report.Results.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_FallbackPath_NullDecision_SynthesizesDefaultVerdict()
    {
        var entry = new NotificationOutboxEntry(
            id: Guid.NewGuid(),
            workItemId: Guid.NewGuid(),
            userId: "user-xyz",
            sourceType: "TeamsMessage",
            title: "Legacy alert",
            priority: 3.0,
            triggerRule: "legacy_rule",
            createdAt: DateTimeOffset.UtcNow,
            dispatchedAt: null);

        var store = Substitute.For<INotificationOutboxStore>();
        store.GetPendingAsync(10, Arg.Any<CancellationToken>()).Returns(new[] { entry });

        var dispatcher = Substitute.For<IWorkItemNotificationDispatcher>();

        var services = new ServiceCollection();
        services.AddSingleton(store);
        services.AddSingleton(dispatcher);
        await using var provider = services.BuildServiceProvider();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var worker = new WorkItemNotificationWorker(
            scopeFactory,
            Substitute.For<ILogger<WorkItemNotificationWorker>>());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(300);

        await dispatcher.Received(1).DispatchAsync(
            Arg.Is<NotificationOutboxEntry>(e => e.Id == entry.Id),
            Arg.Is<InterruptionVerdict>(v =>
                v.Decision == InterruptionDecision.InterruptNow &&
                v.Explanation == null &&
                v.Report.Results.Count == 0 &&
                v.TriggerRule == "legacy_rule"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_NullRuleResults_CreatesEmptyReport()
    {
        var entry = new NotificationOutboxEntry(
            id: Guid.NewGuid(),
            workItemId: Guid.NewGuid(),
            userId: "user-abc",
            sourceType: "TeamsMessage",
            title: "No rules alert",
            priority: 5.0,
            triggerRule: "some_rule",
            createdAt: DateTimeOffset.UtcNow,
            dispatchedAt: null,
            explanation: "No rules",
            decision: "InterruptNow",
            targetUserId: "user-abc",
            ruleResults: null);

        var store = Substitute.For<INotificationOutboxStore>();
        store.GetPendingAsync(10, Arg.Any<CancellationToken>()).Returns(new[] { entry });

        var dispatcher = Substitute.For<IWorkItemNotificationDispatcher>();

        var services = new ServiceCollection();
        services.AddSingleton(store);
        services.AddSingleton(dispatcher);
        await using var provider = services.BuildServiceProvider();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var worker = new WorkItemNotificationWorker(
            scopeFactory,
            Substitute.For<ILogger<WorkItemNotificationWorker>>());

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await worker.StartAsync(cts.Token);
        await Task.Delay(300);

        await dispatcher.Received(1).DispatchAsync(
            Arg.Is<NotificationOutboxEntry>(e => e.Id == entry.Id),
            Arg.Is<InterruptionVerdict>(v =>
                v.Decision == InterruptionDecision.InterruptNow &&
                v.Explanation == "No rules" &&
                v.Report.Results.Count == 0),
            Arg.Any<CancellationToken>());
    }
}
