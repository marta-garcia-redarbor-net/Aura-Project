using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.ConnectorExecution;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.UnitTests.ConnectorExecution;

public class ExecuteConnectorUseCaseWorkItemTests
{
    [Fact]
    public async Task ExecuteAsync_DrainsBufferAfterAdapterExecution_AndPersistsAllItems()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>()).Returns((IngestionCheckpoint?)null);

        var adapterResult = new ConnectorExecutionResult(identity, 2, ConnectorExecutionStatus.Success, MaxProcessedAt: DateTimeOffset.UtcNow);
        var adapter = new StubAdapter(adapterResult);

        var buffer = Substitute.For<IWorkItemBuffer>();
        var items = new[]
        {
            CreateWorkItem("msg-1"),
            CreateWorkItem("msg-2")
        };
        buffer.Drain().Returns(items);

        var store = Substitute.For<IWorkItemStore>();
        store.SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(WorkItemPersistenceResult.Success());

        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            buffer,
            store,
            Substitute.For<ILogger<ExecuteConnectorUseCase>>(),
            () => DateTimeOffset.UtcNow);

        var result = await useCase.ExecuteAsync(identity, CancellationToken.None);

        _ = buffer.Received(1).Drain();
        await store.Received(2).SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>());
        Assert.Equal(ConnectorExecutionStatus.Success, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnyPersistenceFails_UpgradesResultToPartialFailure()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>()).Returns((IngestionCheckpoint?)null);

        var adapter = new StubAdapter(new ConnectorExecutionResult(identity, 2, ConnectorExecutionStatus.Success, MaxProcessedAt: DateTimeOffset.UtcNow));
        var buffer = Substitute.For<IWorkItemBuffer>();
        var items = new[] { CreateWorkItem("msg-1"), CreateWorkItem("msg-2") };
        buffer.Drain().Returns(items);

        var store = Substitute.For<IWorkItemStore>();
        store.SaveAsync(items[0], Arg.Any<CancellationToken>())
            .Returns(WorkItemPersistenceResult.Success());
        store.SaveAsync(items[1], Arg.Any<CancellationToken>())
            .Returns(WorkItemPersistenceResult.Failure("store-unavailable"));

        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            buffer,
            store,
            Substitute.For<ILogger<ExecuteConnectorUseCase>>(),
            () => DateTimeOffset.UtcNow);

        var result = await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.Equal(ConnectorExecutionStatus.PartialFailure, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
        Assert.Contains("store-unavailable", result.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InterruptNow_PersistsFullVerdict()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>()).Returns((IngestionCheckpoint?)null);

        var adapter = new StubAdapter(new ConnectorExecutionResult(identity, 1, ConnectorExecutionStatus.Success));
        var buffer = Substitute.For<IWorkItemBuffer>();
        buffer.Drain().Returns([CreateWorkItem("msg-1")]);
        var store = Substitute.For<IWorkItemStore>();
        store.SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>()).Returns(WorkItemPersistenceResult.Success());

        var rules = new List<RuleResult> { new("vip_sender", true, 9.0, 0.95, "VIP sender") };
        var engine = Substitute.For<IInterruptionPolicyEngine>();
        engine.EvaluateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(new InterruptionVerdict(
                InterruptionDecision.InterruptNow,
                new EvaluationReport(rules),
                triggerRule: "vip_sender",
                explanation: "VIP sender detected — high urgency",
                targetUserId: "user-abc"));

        NotificationOutboxEntry? captured = null;
        var outbox = Substitute.For<INotificationOutboxStore>();
        var dashboardRefresh = Substitute.For<IDashboardRefreshDispatcher>();
        outbox.EnqueueAsync(Arg.Any<NotificationOutboxEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => captured = callInfo.Arg<NotificationOutboxEntry>());

        var semanticOutboxRepo = Substitute.For<ISemanticOutboxRepository>();
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore, [adapter], buffer, store, engine, dashboardRefresh, outbox, semanticOutboxRepo,
            Substitute.For<ILogger<ExecuteConnectorUseCase>>());

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("vip_sender", captured!.TriggerRule);
        Assert.Equal("VIP sender detected — high urgency", captured.Explanation);
        Assert.Equal("InterruptNow", captured.Decision);
        Assert.Equal("user-abc", captured.TargetUserId);
        Assert.NotNull(captured.RuleResults);
        Assert.Contains("vip_sender", captured.RuleResults, StringComparison.Ordinal);
        Assert.Contains("0.95", captured.RuleResults, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InterruptNow_RuleResultsSerializesReportJson()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>()).Returns((IngestionCheckpoint?)null);

        var adapter = new StubAdapter(new ConnectorExecutionResult(identity, 1, ConnectorExecutionStatus.Success));
        var buffer = Substitute.For<IWorkItemBuffer>();
        buffer.Drain().Returns([CreateWorkItem("msg-1")]);
        var store = Substitute.For<IWorkItemStore>();
        store.SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>()).Returns(WorkItemPersistenceResult.Success());

        var rules = new List<RuleResult>
        {
            new("rule_a", true, 8.0, 0.9, "Reason A"),
            new("rule_b", false, 2.0, 0.3, null)
        };
        var engine = Substitute.For<IInterruptionPolicyEngine>();
        engine.EvaluateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(new InterruptionVerdict(
                InterruptionDecision.InterruptNow,
                new EvaluationReport(rules),
                triggerRule: "multi_rule",
                targetUserId: "user-abc"));

        NotificationOutboxEntry? captured = null;
        var outbox = Substitute.For<INotificationOutboxStore>();
        var dashboardRefresh = Substitute.For<IDashboardRefreshDispatcher>();
        outbox.EnqueueAsync(Arg.Any<NotificationOutboxEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(callInfo => captured = callInfo.Arg<NotificationOutboxEntry>());

        var semanticOutboxRepo = Substitute.For<ISemanticOutboxRepository>();
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore, [adapter], buffer, store, engine, dashboardRefresh, outbox, semanticOutboxRepo,
            Substitute.For<ILogger<ExecuteConnectorUseCase>>());

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.NotNull(captured!.RuleResults);
        Assert.Contains("rule_a", captured.RuleResults, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("rule_b", captured.RuleResults, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Reason A", captured.RuleResults, StringComparison.Ordinal);
        Assert.True(captured.RuleResults.Length > 20, "RuleResults should contain meaningful JSON");
    }

    private static WorkItem CreateWorkItem(string externalId) =>
        new(
            externalId,
            $"title-{externalId}",
            "messages",
            WorkItemSourceType.TeamsMessage,
            WorkItemPriority.Medium,
            new Dictionary<string, string>());

    private sealed class StubAdapter(ConnectorExecutionResult result) : IConnectorAdapter
    {
        public string ConnectorName => "teams";

        public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
            => Task.FromResult(result);
    }
}
