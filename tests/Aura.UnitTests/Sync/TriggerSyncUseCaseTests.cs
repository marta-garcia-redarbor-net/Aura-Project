using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.IngestionSync;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aura.UnitTests.Sync;

public class TriggerSyncUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_AllConnectorsSucceed_ReturnsAggregatedSuccess()
    {
        var teamsAdapter = CreateAdapter("teams", ConnectorExecutionStatus.Success, 5);
        var outlookAdapter = CreateAdapter("outlook", ConnectorExecutionStatus.Success, 3);
        var syncStateStore = Substitute.For<ISyncStateStore>();
        var useCase = CreateUseCase([teamsAdapter, outlookAdapter], syncStateStore);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, result.Results.Count);
        Assert.Equal("teams", result.Results[0].Source);
        Assert.Equal("success", result.Results[0].Status);
        Assert.Equal(5, result.Results[0].ItemCount);
        Assert.Equal("outlook", result.Results[1].Source);
        Assert.Equal("success", result.Results[1].Status);
        Assert.Equal(3, result.Results[1].ItemCount);
    }

    [Fact]
    public async Task ExecuteAsync_OneConnectorFails_OtherContinues_PartialDegradation()
    {
        var teamsAdapter = CreateAdapter("teams", ConnectorExecutionStatus.Failure, 0, "auth_required");
        var outlookAdapter = CreateAdapter("outlook", ConnectorExecutionStatus.Success, 7);
        var syncStateStore = Substitute.For<ISyncStateStore>();
        var useCase = CreateUseCase([teamsAdapter, outlookAdapter], syncStateStore);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, result.Results.Count);
        Assert.Equal("auth_required", result.Results[0].Status);
        Assert.Equal("auth_required", result.Results[0].FailureReason);
        Assert.Equal("success", result.Results[1].Status);
        Assert.Equal(7, result.Results[1].ItemCount);
    }

    [Fact]
    public async Task ExecuteAsync_ConnectorThrowsWithReAuthMessage_ReportsAuthRequired()
    {
        var teamsAdapter = Substitute.For<IConnectorAdapter>();
        teamsAdapter.ConnectorName.Returns("teams");
        teamsAdapter.ExecuteAsync(Arg.Any<ConnectorExecutionRequest>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Re-auth needed — no_account found."));

        var outlookAdapter = CreateAdapter("outlook", ConnectorExecutionStatus.Success, 2);
        var syncStateStore = Substitute.For<ISyncStateStore>();
        var useCase = CreateUseCase([teamsAdapter, outlookAdapter], syncStateStore);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, result.Results.Count);
        Assert.Equal("auth_required", result.Results[0].Status);
        Assert.Contains("Re-auth", result.Results[0].FailureReason);
        Assert.Equal("success", result.Results[1].Status);
    }

    [Fact]
    public async Task ExecuteAsync_UpdatesSyncStateStore_PerSource()
    {
        var teamsAdapter = CreateAdapter("teams", ConnectorExecutionStatus.Success, 3);
        var syncStateStore = Substitute.For<ISyncStateStore>();
        var useCase = CreateUseCase([teamsAdapter], syncStateStore);

        await useCase.ExecuteAsync(CancellationToken.None);

        await syncStateStore.Received(1).UpdateAsync("teams", Arg.Any<SourceSyncState>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_NoConnectors_ReturnsEmptyResults()
    {
        var syncStateStore = Substitute.For<ISyncStateStore>();
        var useCase = CreateUseCase([], syncStateStore);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task ExecuteAsync_DrainsBufferAndPersistsItems_WhenStoreProvided()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var store = Substitute.For<IWorkItemStore>();
        var workItems = new[]
        {
            new Aura.Domain.WorkItems.WorkItem("ext-1", "Test item", "teams",
                Aura.Domain.WorkItems.WorkItemSourceType.TeamsMessage,
                Aura.Domain.WorkItems.WorkItemPriority.High,
                new Dictionary<string, string> { ["teams.sender"] = "Alice" }),
            new Aura.Domain.WorkItems.WorkItem("ext-2", "Another item", "teams",
                Aura.Domain.WorkItems.WorkItemSourceType.TeamsMessage,
                Aura.Domain.WorkItems.WorkItemPriority.Medium,
                new Dictionary<string, string> { ["teams.snippet"] = "Hello" })
        };
        buffer.Drain().Returns(workItems.ToList().AsReadOnly());
        store.SaveAsync(Arg.Any<Aura.Domain.WorkItems.WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(WorkItemPersistenceResult.Success()));

        var teamsAdapter = CreateAdapter("teams", ConnectorExecutionStatus.Success, 2);
        var syncStateStore = Substitute.For<ISyncStateStore>();
        var useCase = new TriggerSyncUseCase(
            [teamsAdapter], syncStateStore,
            NullLogger<TriggerSyncUseCase>.Instance,
            buffer, store);

        await useCase.ExecuteAsync(CancellationToken.None);

        buffer.Received(1).Drain();
        await store.Received(2).SaveAsync(Arg.Any<Aura.Domain.WorkItems.WorkItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithUserOid_PropagatesOidToConnectorExecutionRequest()
    {
        var adapter = Substitute.For<IConnectorAdapter>();
        adapter.ConnectorName.Returns("outlook");

        CheckpointIdentity? capturedIdentity = null;
        adapter.ExecuteAsync(Arg.Any<ConnectorExecutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                capturedIdentity = callInfo.Arg<ConnectorExecutionRequest>().Identity;
                return Task.FromResult(new ConnectorExecutionResult(
                    capturedIdentity,
                    1,
                    ConnectorExecutionStatus.Success));
            });

        var syncStateStore = Substitute.For<ISyncStateStore>();
        var useCase = CreateUseCase([adapter], syncStateStore);

        await useCase.ExecuteAsync("oid-test-1", CancellationToken.None);

        Assert.NotNull(capturedIdentity);
        Assert.Equal("oid-test-1", capturedIdentity!.UserOid);
    }

    private static IConnectorAdapter CreateAdapter(string name, ConnectorExecutionStatus status, int itemCount, string? failureReason = null)
    {
        var adapter = Substitute.For<IConnectorAdapter>();
        adapter.ConnectorName.Returns(name);
        adapter.ExecuteAsync(Arg.Any<ConnectorExecutionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ConnectorExecutionResult(
                new CheckpointIdentity(name, name == "teams" ? "messages" : "inbox", "acme"),
                itemCount,
                status,
                failureReason,
                DateTimeOffset.UtcNow)));
        return adapter;
    }

    private static TriggerSyncUseCase CreateUseCase(IConnectorAdapter[] adapters, ISyncStateStore syncStateStore)
        => new(adapters, syncStateStore, NullLogger<TriggerSyncUseCase>.Instance);
}
