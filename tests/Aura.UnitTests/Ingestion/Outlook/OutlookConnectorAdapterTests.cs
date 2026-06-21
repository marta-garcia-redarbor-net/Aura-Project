using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Outlook;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Aura.UnitTests.Ingestion.Outlook;

public class OutlookConnectorAdapterTests
{
    [Fact]
    public async Task ExecuteAsync_MapsAndEnqueuesAllValidFixtures()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var fixtures = new[]
        {
            new OutlookEmailDto { ExternalId = "mail-1", Subject = "one", Importance = "High", SenderAddress = "ceo@aura.dev", BodyPreview = "incident" },
            new OutlookEmailDto { ExternalId = "mail-2", Subject = "two", Importance = "Normal", SenderAddress = "manager@aura.dev", BodyPreview = "follow up" }
        };

        var adapter = new OutlookConnectorAdapter(
            NullLogger<OutlookConnectorAdapter>.Instance,
            buffer,
            new OutlookWorkItemMapper(),
            () => fixtures);

        var result = await adapter.ExecuteAsync(CreateRequest(), CancellationToken.None);

        buffer.Received(2).Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>());
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Success, result.Status);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsInvalidFixture_ContinuesBatch_WithPartialFailure()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var fixtures = new[]
        {
            new OutlookEmailDto { ExternalId = "mail-1", Subject = "one", Importance = "High", SenderAddress = "ceo@aura.dev", BodyPreview = "incident" },
            new OutlookEmailDto { ExternalId = null, Subject = "invalid" },
            new OutlookEmailDto { ExternalId = "mail-3", Subject = "three", Importance = "Low", SenderAddress = "unknown@aura.dev", BodyPreview = "note" }
        };

        var adapter = new OutlookConnectorAdapter(
            NullLogger<OutlookConnectorAdapter>.Instance,
            buffer,
            new OutlookWorkItemMapper(),
            () => fixtures);

        var result = await adapter.ExecuteAsync(CreateRequest(), CancellationToken.None);

        buffer.Received(2).Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>());
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.PartialFailure, result.Status);
        Assert.Equal("Skipped 1 invalid Outlook payload(s).", result.FailureReason);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutFixtureProvider_UsesDefaultFixturePath()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var adapter = new OutlookConnectorAdapter(
            NullLogger<OutlookConnectorAdapter>.Instance,
            buffer,
            new OutlookWorkItemMapper());

        var result = await adapter.ExecuteAsync(CreateRequest(), CancellationToken.None);

        buffer.Received(2).Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>());
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.PartialFailure, result.Status);
        Assert.Equal("Skipped 1 invalid Outlook payload(s).", result.FailureReason);
    }

    private static ConnectorExecutionRequest CreateRequest() =>
        new(
            new CheckpointIdentity("outlook", "inbox", "acme"),
            new DateTimeOffset(2026, 06, 21, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 06, 21, 23, 59, 59, TimeSpan.Zero));
}
