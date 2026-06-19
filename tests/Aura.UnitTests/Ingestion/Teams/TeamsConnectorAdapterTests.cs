using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Teams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aura.UnitTests.Ingestion.Teams;

public class TeamsConnectorAdapterTests
{
    [Fact]
    public async Task ExecuteAsync_MapsAndEnqueuesAllValidFixtures()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var fixtures = new[]
        {
            new TeamsMessageDto { ExternalId = "msg-1", Title = "t1", Source = "messages", Priority = "high" },
            new TeamsMessageDto { ExternalId = "msg-2", Title = "t2", Source = "messages", Priority = "low" }
        };
        var adapter = new TeamsConnectorAdapter(NullLogger<TeamsConnectorAdapter>.Instance, buffer, new TeamsWorkItemMapper(), () => fixtures);
        var request = CreateRequest();

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        buffer.Received(2).Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>());
        buffer.Received(2).Enqueue(Arg.Is<Aura.Domain.WorkItems.WorkItem>(item => item.SourceType == Aura.Domain.WorkItems.WorkItemSourceType.TeamsMessage));
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Success, result.Status);
        Assert.Equal(request.WindowEnd, result.MaxProcessedAt);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsInvalidFixture_ContinuesBatch_WithPartialFailure()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var fixtures = new[]
        {
            new TeamsMessageDto { ExternalId = "msg-1", Title = "t1", Source = "messages", Priority = "high" },
            new TeamsMessageDto { ExternalId = null, Title = "missing id", Source = "messages", Priority = "low" },
            new TeamsMessageDto { ExternalId = "msg-3", Title = "t3", Source = "messages", Priority = "unknown" }
        };
        var adapter = new TeamsConnectorAdapter(NullLogger<TeamsConnectorAdapter>.Instance, buffer, new TeamsWorkItemMapper(), () => fixtures);
        var request = CreateRequest();

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        buffer.Received(2).Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>());
        buffer.Received(2).Enqueue(Arg.Is<Aura.Domain.WorkItems.WorkItem>(item => item.SourceType == Aura.Domain.WorkItems.WorkItemSourceType.TeamsMessage));
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.PartialFailure, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutFixtureProvider_UsesDefaultFixturePath()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var adapter = new TeamsConnectorAdapter(NullLogger<TeamsConnectorAdapter>.Instance, buffer, new TeamsWorkItemMapper());
        var request = CreateRequest();

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        buffer.Received(2).Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>());
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.PartialFailure, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
    }

    private static ConnectorExecutionRequest CreateRequest() =>
        new(
            new CheckpointIdentity("teams", "messages", "acme"),
            new DateTimeOffset(2026, 06, 19, 00, 00, 00, TimeSpan.Zero),
            new DateTimeOffset(2026, 06, 19, 23, 59, 59, TimeSpan.Zero));
}
