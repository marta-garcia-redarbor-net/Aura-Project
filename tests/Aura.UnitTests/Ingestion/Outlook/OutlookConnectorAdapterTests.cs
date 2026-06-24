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

    [Fact]
    public async Task ExecuteAsync_WithSourceProvider_UsesProviderInsteadOfFixtures()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var provider = Substitute.For<IMessageSourceProvider<OutlookEmailDto>>();
        var providerPayloads = new[]
        {
            new OutlookEmailDto
            {
                ExternalId = "graph-mail-1", Subject = "Critical escalation", Importance = "High",
                SenderAddress = "ceo@aura.dev", BodyPreview = "production down immediate action required",
                WebLink = "https://outlook.office.com/mail/id/AAA"
            },
            new OutlookEmailDto
            {
                ExternalId = "graph-mail-2", Subject = "Weekly sync", Importance = "Normal",
                SenderAddress = "manager@aura.dev", BodyPreview = "review today",
                WebLink = "https://outlook.office.com/mail/id/BBB"
            }
        };
        var request = CreateRequest();
        provider.FetchAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OutlookEmailDto>>(providerPayloads));

        var adapter = new OutlookConnectorAdapter(
            NullLogger<OutlookConnectorAdapter>.Instance, buffer, new OutlookWorkItemMapper(),
            fixtureProvider: () => throw new InvalidOperationException("Fixture must not be called when provider exists"),
            sourceProvider: provider);

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        await provider.Received(1).FetchAsync(request, Arg.Any<CancellationToken>());
        buffer.Received(2).Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>());
        Assert.Equal(2, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Success, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceProvider_MapsMetadataFieldsCorrectly()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var capturedItems = new List<Aura.Domain.WorkItems.WorkItem>();
        buffer.When(b => b.Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>()))
            .Do(ci => capturedItems.Add(ci.Arg<Aura.Domain.WorkItems.WorkItem>()));

        var provider = Substitute.For<IMessageSourceProvider<OutlookEmailDto>>();
        var providerPayloads = new[]
        {
            new OutlookEmailDto
            {
                ExternalId = "graph-mail-1", Subject = "Urgent incident escalation", Importance = "High",
                SenderAddress = "ceo@aura.dev", BodyPreview = "production down immediate action required",
                WebLink = "https://outlook.office.com/mail/id/AAA",
                CorrelationId = "corr-1", ConversationId = "conv-1"
            }
        };
        var request = CreateRequest();
        provider.FetchAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OutlookEmailDto>>(providerPayloads));

        var adapter = new OutlookConnectorAdapter(
            NullLogger<OutlookConnectorAdapter>.Instance, buffer, new OutlookWorkItemMapper(),
            sourceProvider: provider);

        await adapter.ExecuteAsync(request, CancellationToken.None);

        Assert.Single(capturedItems);
        var item = capturedItems[0];
        Assert.Equal("ceo@aura.dev", item.Metadata["outlook.sender"]);
        Assert.Equal("production down immediate action required", item.Metadata["outlook.snippet"]);
        Assert.Equal("https://outlook.office.com/mail/id/AAA", item.Metadata["outlook.deepLink"]);
    }

    private static ConnectorExecutionRequest CreateRequest() =>
        new(
            new CheckpointIdentity("outlook", "inbox", "acme"),
            new DateTimeOffset(2026, 06, 21, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 06, 21, 23, 59, 59, TimeSpan.Zero));
}
