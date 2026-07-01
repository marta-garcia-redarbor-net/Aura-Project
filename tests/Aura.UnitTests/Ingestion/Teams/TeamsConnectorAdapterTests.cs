using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
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

        buffer.Received(3).Enqueue(Arg.Any<Aura.Domain.WorkItems.WorkItem>());
        Assert.Equal(3, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Success, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_WithSourceProvider_UsesProviderInsteadOfFixtures()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var provider = Substitute.For<IMessageSourceProvider<TeamsMessageDto>>();
        var providerPayloads = new[]
        {
            new TeamsMessageDto
            {
                ExternalId = "graph-msg-1", Title = "From Graph provider", Source = "messages",
                Priority = "high", Sender = "John Doe", BodyPreview = "Hello team", WebUrl = "https://teams.microsoft.com/msg/1"
            },
            new TeamsMessageDto
            {
                ExternalId = "graph-msg-2", Title = "Another Graph message", Source = "messages",
                Priority = "medium", Sender = "Jane Smith", BodyPreview = "FYI", WebUrl = "https://teams.microsoft.com/msg/2"
            }
        };
        var request = CreateRequest();
        provider.FetchAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TeamsMessageDto>>(providerPayloads));

        // Fixture is also provided, but should NOT be used when sourceProvider is non-null
        var adapter = new TeamsConnectorAdapter(
            NullLogger<TeamsConnectorAdapter>.Instance, buffer, new TeamsWorkItemMapper(),
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

        var provider = Substitute.For<IMessageSourceProvider<TeamsMessageDto>>();
        var providerPayloads = new[]
        {
            new TeamsMessageDto
            {
                ExternalId = "graph-msg-1", Title = "PR Review needed", Source = "messages",
                Priority = "high", Sender = "Alice", BodyPreview = "Please review PR #42", WebUrl = "https://teams.microsoft.com/msg/42"
            }
        };
        var request = CreateRequest();
        provider.FetchAsync(request, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TeamsMessageDto>>(providerPayloads));

        var adapter = new TeamsConnectorAdapter(
            NullLogger<TeamsConnectorAdapter>.Instance, buffer, new TeamsWorkItemMapper(),
            sourceProvider: provider);

        await adapter.ExecuteAsync(request, CancellationToken.None);

        Assert.Single(capturedItems);
        var item = capturedItems[0];
        Assert.Equal("Alice", item.Metadata["teams.sender"]);
        Assert.Equal("Please review PR #42", item.Metadata["teams.snippet"]);
        Assert.Equal("https://teams.microsoft.com/msg/42", item.Metadata["teams.deepLink"]);
    }

    [Fact]
    public async Task ExecuteAsync_ChatFullyRead_CallsMarkAutoCompleted()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var capturedItems = new List<WorkItem>();
        buffer.When(b => b.Enqueue(Arg.Any<WorkItem>()))
            .Do(ci => capturedItems.Add(ci.Arg<WorkItem>()));

        var readAt = new DateTimeOffset(2026, 06, 30, 15, 00, 00, TimeSpan.Zero);
        var msgAt = new DateTimeOffset(2026, 06, 30, 14, 00, 00, TimeSpan.Zero);
        var fixtures = new[]
        {
            new TeamsMessageDto
            {
                ExternalId = "chat-read-1",
                Title = "Fully read chat",
                Source = "chats",
                Priority = "medium",
                LastMessageReadAt = readAt,
                LastMessageAt = msgAt,
                UnreadCount = 0
            }
        };
        var adapter = new TeamsConnectorAdapter(
            NullLogger<TeamsConnectorAdapter>.Instance, buffer, new TeamsWorkItemMapper(), () => fixtures);
        var request = CreateRequest();

        var result = await adapter.ExecuteAsync(request, CancellationToken.None);

        Assert.Single(capturedItems);
        var item = capturedItems[0];
        Assert.Equal(WorkItemStatus.Completed, item.Status);
        Assert.Equal(WorkItemSourceType.TeamsChat, item.SourceType);
    }

    [Fact]
    public async Task ExecuteAsync_ChatNullLastMessageReadAt_TreatedAsUnread()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var capturedItems = new List<WorkItem>();
        buffer.When(b => b.Enqueue(Arg.Any<WorkItem>()))
            .Do(ci => capturedItems.Add(ci.Arg<WorkItem>()));

        var fixtures = new[]
        {
            new TeamsMessageDto
            {
                ExternalId = "chat-unread-1",
                Title = "Unread chat",
                Source = "chats",
                Priority = "high",
                LastMessageReadAt = null,
                LastMessageAt = new DateTimeOffset(2026, 06, 30, 14, 00, 00, TimeSpan.Zero),
                UnreadCount = 3
            }
        };
        var adapter = new TeamsConnectorAdapter(
            NullLogger<TeamsConnectorAdapter>.Instance, buffer, new TeamsWorkItemMapper(), () => fixtures);
        var request = CreateRequest();

        await adapter.ExecuteAsync(request, CancellationToken.None);

        Assert.Single(capturedItems);
        var item = capturedItems[0];
        Assert.Equal(WorkItemStatus.Pending, item.Status);
        Assert.Equal(WorkItemSourceType.TeamsChat, item.SourceType);
    }

    [Fact]
    public async Task ExecuteAsync_ChatPartiallyRead_StaysPending()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var capturedItems = new List<WorkItem>();
        buffer.When(b => b.Enqueue(Arg.Any<WorkItem>()))
            .Do(ci => capturedItems.Add(ci.Arg<WorkItem>()));

        var readAt = new DateTimeOffset(2026, 06, 30, 13, 00, 00, TimeSpan.Zero);
        var msgAt = new DateTimeOffset(2026, 06, 30, 14, 00, 00, TimeSpan.Zero);
        var fixtures = new[]
        {
            new TeamsMessageDto
            {
                ExternalId = "chat-partial-1",
                Title = "Partially read chat",
                Source = "chats",
                Priority = "medium",
                LastMessageReadAt = readAt,
                LastMessageAt = msgAt,
                UnreadCount = 2
            }
        };
        var adapter = new TeamsConnectorAdapter(
            NullLogger<TeamsConnectorAdapter>.Instance, buffer, new TeamsWorkItemMapper(), () => fixtures);
        var request = CreateRequest();

        await adapter.ExecuteAsync(request, CancellationToken.None);

        Assert.Single(capturedItems);
        var item = capturedItems[0];
        Assert.Equal(WorkItemStatus.Pending, item.Status);
    }

    [Fact]
    public async Task ExecuteAsync_NonChatSource_DoesNotAutoDismiss()
    {
        var buffer = Substitute.For<IWorkItemBuffer>();
        var capturedItems = new List<WorkItem>();
        buffer.When(b => b.Enqueue(Arg.Any<WorkItem>()))
            .Do(ci => capturedItems.Add(ci.Arg<WorkItem>()));

        var fixtures = new[]
        {
            new TeamsMessageDto
            {
                ExternalId = "msg-10",
                Title = "Regular message",
                Source = "messages",
                Priority = "medium",
                LastMessageReadAt = DateTimeOffset.UtcNow,
                LastMessageAt = DateTimeOffset.UtcNow.AddHours(-1),
                UnreadCount = 0
            }
        };
        var adapter = new TeamsConnectorAdapter(
            NullLogger<TeamsConnectorAdapter>.Instance, buffer, new TeamsWorkItemMapper(), () => fixtures);
        var request = CreateRequest();

        await adapter.ExecuteAsync(request, CancellationToken.None);

        Assert.Single(capturedItems);
        var item = capturedItems[0];
        Assert.Equal(WorkItemStatus.Pending, item.Status);
        Assert.Equal(WorkItemSourceType.TeamsMessage, item.SourceType);
    }

    private static ConnectorExecutionRequest CreateRequest() =>
        new(
            new CheckpointIdentity("teams", "messages", "acme"),
            new DateTimeOffset(2026, 06, 19, 00, 00, 00, TimeSpan.Zero),
            new DateTimeOffset(2026, 06, 19, 23, 59, 59, TimeSpan.Zero));
}
