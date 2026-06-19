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
