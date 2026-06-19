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
}
