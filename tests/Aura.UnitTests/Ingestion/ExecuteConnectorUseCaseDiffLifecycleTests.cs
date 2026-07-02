using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.ConnectorExecution;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.UnitTests.Ingestion;

public class ExecuteConnectorUseCaseDiffLifecycleTests
{
    private readonly IIngestionCheckpointStore _checkpointStore;
    private readonly IWorkItemBuffer _buffer;
    private readonly IWorkItemStore _store;
    private readonly ILogger<ExecuteConnectorUseCase> _logger;

    public ExecuteConnectorUseCaseDiffLifecycleTests()
    {
        _checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        _checkpointStore.GetAsync(Arg.Any<CheckpointIdentity>(), Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        _buffer = Substitute.For<IWorkItemBuffer>();
        _buffer.Drain().Returns([]);

        _store = Substitute.For<IWorkItemStore>();
        _store.SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(WorkItemPersistenceResult.Success());

        _logger = Substitute.For<ILogger<ExecuteConnectorUseCase>>();
    }

    private ExecuteConnectorUseCase CreateUseCase(IConnectorAdapter adapter)
        => new(_checkpointStore, new[] { adapter }, _buffer, _store, _logger);

    [Fact]
    public async Task ExecuteAsync_OutlookDiff_MarksAbsentAsCompleted()
    {
        // Arrange
        var identity = new CheckpointIdentity("outlook", "inbox", "acme");
        var adapter = new StubConnectorAdapter("outlook", new ConnectorExecutionResult(
            identity, 2, ConnectorExecutionStatus.Success, MaxProcessedAt: DateTimeOffset.UtcNow));

        var batchItems = new[] { CreateWorkItem("batch-1", "Title 1"), CreateWorkItem("batch-2", "Title 2") };
        _buffer.Drain().Returns(batchItems);

        // 3 pending in store: batch-1, batch-2, absent-3
        _store.GetPendingExternalIdsAsync(WorkItemSourceType.OutlookEmail, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "batch-1", "batch-2", "absent-3" });

        var useCase = CreateUseCase(adapter);

        // Act
        await useCase.ExecuteAsync(identity, CancellationToken.None);

        // Assert — absent-3 should be marked as completed
        await _store.Received(1).MarkCompletedAsync(
            Arg.Is<IReadOnlySet<string>>(s => s.Count == 1 && s.Contains("absent-3")),
            WorkItemSourceType.OutlookEmail,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_OutlookGraphError_SkipsDiff()
    {
        // Arrange
        var identity = new CheckpointIdentity("outlook", "inbox", "acme");
        var adapter = new StubConnectorAdapter("outlook", new ConnectorExecutionResult(
            identity, 0, ConnectorExecutionStatus.Failure, "Graph HTTP 403"));

        var useCase = CreateUseCase(adapter);

        // Act
        await useCase.ExecuteAsync(identity, CancellationToken.None);

        // Assert — no MarkCompletedAsync should be called
        await _store.DidNotReceiveWithAnyArgs().MarkCompletedAsync(default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_NonOutlookConnector_SkipsDiff()
    {
        // Arrange
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var adapter = new StubConnectorAdapter("teams", new ConnectorExecutionResult(
            identity, 2, ConnectorExecutionStatus.Success, MaxProcessedAt: DateTimeOffset.UtcNow));

        var batchItems = new[] { CreateWorkItem("msg-1", "Teams 1") };
        _buffer.Drain().Returns(batchItems);

        var useCase = CreateUseCase(adapter);

        // Act
        await useCase.ExecuteAsync(identity, CancellationToken.None);

        // Assert — no MarkCompletedAsync for non-Outlook
        await _store.DidNotReceiveWithAnyArgs().MarkCompletedAsync(default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_OutlookInboxZero_MarksAllPendingAsCompleted()
    {
        // Arrange
        var identity = new CheckpointIdentity("outlook", "inbox", "acme");
        var adapter = new StubConnectorAdapter("outlook", new ConnectorExecutionResult(
            identity, 0, ConnectorExecutionStatus.Success, MaxProcessedAt: DateTimeOffset.UtcNow));

        // Empty batch (inbox zero)
        _buffer.Drain().Returns([]);

        _store.GetPendingExternalIdsAsync(WorkItemSourceType.OutlookEmail, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "old-1", "old-2" });

        var useCase = CreateUseCase(adapter);

        // Act
        await useCase.ExecuteAsync(identity, CancellationToken.None);

        // Assert — all pending should be marked completed
        await _store.Received(1).MarkCompletedAsync(
            Arg.Is<IReadOnlySet<string>>(s => s.Count == 2 && s.Contains("old-1") && s.Contains("old-2")),
            WorkItemSourceType.OutlookEmail,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_OutlookNoPendingPending_SkipsMarkCompleted()
    {
        // Arrange
        var identity = new CheckpointIdentity("outlook", "inbox", "acme");
        var adapter = new StubConnectorAdapter("outlook", new ConnectorExecutionResult(
            identity, 0, ConnectorExecutionStatus.Success, MaxProcessedAt: DateTimeOffset.UtcNow));

        _buffer.Drain().Returns([]);
        _store.GetPendingExternalIdsAsync(WorkItemSourceType.OutlookEmail, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>()); // no pending items

        var useCase = CreateUseCase(adapter);

        // Act
        await useCase.ExecuteAsync(identity, CancellationToken.None);

        // Assert — no MarkCompletedAsync when nothing pending
        await _store.DidNotReceiveWithAnyArgs().MarkCompletedAsync(default!, default, default);
    }

    private static WorkItem CreateWorkItem(string externalId, string title) =>
        new(externalId, title, "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium,
            new Dictionary<string, string>());

    private sealed class StubConnectorAdapter(string connectorName, ConnectorExecutionResult result) : IConnectorAdapter
    {
        public string ConnectorName => connectorName;

        public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
            => Task.FromResult(result);
    }
}
