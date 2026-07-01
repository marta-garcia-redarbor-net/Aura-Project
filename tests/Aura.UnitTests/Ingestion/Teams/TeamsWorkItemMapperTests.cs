using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Connectors.Teams;

namespace Aura.UnitTests.Ingestion.Teams;

public class TeamsWorkItemMapperTests
{
    private readonly TeamsWorkItemMapper _mapper = new();

    [Fact]
    public void TryMap_ValidPayload_MapsCanonicalWorkItem()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "msg-1",
            Title = "Investigate incident",
            Source = "messages",
            Priority = "high",
            TeamId = "team-a",
            ChannelId = "channel-a",
            MessageUrl = "https://teams/messages/1",
            CapturedAtUtc = new DateTimeOffset(2026, 06, 19, 10, 00, 00, TimeSpan.Zero),
            CorrelationId = "corr-1"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("msg-1", workItem!.ExternalId);
        Assert.Equal("Investigate incident", workItem.Title);
        Assert.Equal("messages", workItem.Source);
        Assert.Equal(WorkItemSourceType.TeamsMessage, workItem.SourceType);
        Assert.Equal(WorkItemPriority.High, workItem.Priority);
    }

    [Fact]
    public void TryMap_MissingOptionalField_UsesSafeDefaultAndRecordsMetadata()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "msg-2",
            Title = "Follow up",
            Source = "messages",
            Priority = "medium",
            TeamId = "team-a",
            ChannelId = "channel-a",
            MessageUrl = null,
            CapturedAtUtc = new DateTimeOffset(2026, 06, 19, 10, 30, 00, TimeSpan.Zero)
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal(WorkItemSourceType.TeamsMessage, workItem!.SourceType);
        Assert.Equal("absent", workItem.Metadata["teams.messageUrl"]);
    }

    [Fact]
    public void TryMap_UnrecognizedPriority_DefaultsToMedium_AndRecordsMetadata()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "msg-3",
            Title = "Backlog item",
            Source = "messages",
            Priority = "urgent",
            TeamId = "team-a",
            ChannelId = "channel-a"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal(WorkItemPriority.Medium, workItem!.Priority);
        Assert.Equal("urgent", workItem.Metadata["teams.priority.raw"]);
        Assert.Equal("defaulted-medium", workItem.Metadata["teams.priority.resolution"]);
    }

    [Fact]
    public void TryMap_AbsentPriority_DefaultsToMedium_AndRecordsMetadata()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "msg-4",
            Title = "Missing priority",
            Source = "messages",
            Priority = null,
            TeamId = "team-a",
            ChannelId = "channel-a"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal(WorkItemPriority.Medium, workItem!.Priority);
        Assert.Equal("absent", workItem.Metadata["teams.priority.raw"]);
        Assert.Equal("defaulted-medium", workItem.Metadata["teams.priority.resolution"]);
    }

    [Fact]
    public void TryMap_MissingTitle_UsesDefault_AndRecordsMetadataTraceability()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "msg-5",
            Title = null,
            Source = "messages",
            Priority = "medium"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("Teams message msg-5", workItem!.Title);
        Assert.Equal("absent", workItem.Metadata["teams.title.raw"]);
        Assert.Equal("defaulted", workItem.Metadata["teams.title.resolution"]);
    }

    [Fact]
    public void TryMap_MissingSource_UsesDefault_AndRecordsMetadataTraceability()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "msg-6",
            Title = "Title",
            Source = null,
            Priority = "medium"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("messages", workItem!.Source);
        Assert.Equal("absent", workItem.Metadata["teams.source.raw"]);
        Assert.Equal("defaulted", workItem.Metadata["teams.source.resolution"]);
    }

    [Fact]
    public void TryMap_MissingExternalId_SkipsItem()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "",
            Title = "Message without id",
            Source = "messages",
            Priority = "low"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.False(mapped);
        Assert.Null(workItem);
    }

    [Fact]
    public void TryMap_ChatSource_MapsTeamsChatSourceType()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "chat-1",
            Title = "General discussion",
            Source = "chats",
            Priority = "medium",
            CapturedAtUtc = new DateTimeOffset(2026, 06, 30, 12, 00, 00, TimeSpan.Zero)
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("chats", workItem!.Source);
        Assert.Equal(WorkItemSourceType.TeamsChat, workItem.SourceType);
    }

    [Fact]
    public void TryMap_ChatSource_MapsChatMetadataFields()
    {
        var lastMsgAt = new DateTimeOffset(2026, 06, 30, 14, 00, 00, TimeSpan.Zero);
        var lastMsgReadAt = new DateTimeOffset(2026, 06, 30, 13, 30, 00, TimeSpan.Zero);
        var message = new TeamsMessageDto
        {
            ExternalId = "chat-2",
            Title = "Support ticket #42",
            Source = "chats",
            Priority = "high",
            LastMessageAt = lastMsgAt,
            LastMessageReadAt = lastMsgReadAt,
            UnreadCount = 3,
            CapturedAtUtc = new DateTimeOffset(2026, 06, 30, 12, 00, 00, TimeSpan.Zero)
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal(WorkItemSourceType.TeamsChat, workItem!.SourceType);
        Assert.Equal(lastMsgAt.ToString("o"), workItem.Metadata["chats.lastMessageAt"]);
        Assert.Equal(lastMsgReadAt.ToString("o"), workItem.Metadata["chats.lastMessageReadAt"]);
        Assert.Equal("3", workItem.Metadata["chats.unreadCount"]);
    }

    [Fact]
    public void TryMap_ChatSource_NullChatFields_OmittedFromMetadata()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "chat-3",
            Title = "Quiet channel",
            Source = "chats",
            Priority = "low",
            LastMessageAt = null,
            LastMessageReadAt = null,
            UnreadCount = 0,
            CapturedAtUtc = new DateTimeOffset(2026, 06, 30, 12, 00, 00, TimeSpan.Zero)
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal(WorkItemSourceType.TeamsChat, workItem!.SourceType);
        Assert.False(workItem.Metadata.ContainsKey("chats.lastMessageAt"));
        Assert.False(workItem.Metadata.ContainsKey("chats.lastMessageReadAt"));
        Assert.Equal("0", workItem.Metadata["chats.unreadCount"]);
    }

    [Fact]
    public void TryMap_MessageSource_StillMapsTeamsMessageSourceType()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "msg-10",
            Title = "Regular message",
            Source = "messages",
            Priority = "medium",
            LastMessageAt = DateTimeOffset.UtcNow,
            LastMessageReadAt = DateTimeOffset.UtcNow,
            UnreadCount = 5,
            CapturedAtUtc = new DateTimeOffset(2026, 06, 30, 12, 00, 00, TimeSpan.Zero)
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal(WorkItemSourceType.TeamsMessage, workItem!.SourceType);
        // Chat fields present but source is "messages" — should NOT add chat metadata
        Assert.False(workItem.Metadata.ContainsKey("messages.lastMessageAt"));
        Assert.False(workItem.Metadata.ContainsKey("messages.lastMessageReadAt"));
        Assert.False(workItem.Metadata.ContainsKey("messages.unreadCount"));
    }
}
