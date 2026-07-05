using Aura.Domain.WorkItems;
using Aura.Application.Models;
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

    [Fact]
    public void TryMap_WritesCanonicalTriageMetadata()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "msg-11",
            Title = "Need response",
            Source = "messages",
            Priority = "high",
            Sender = "Alice Smith",
            BodyPreview = "Please review the incident and respond"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("Alice Smith", workItem!.Metadata[WorkItemSignalKeys.CanonicalSender]);
        Assert.Equal("Please review the incident and respond", workItem.Metadata[WorkItemSignalKeys.CanonicalSnippet]);
        Assert.Equal(SignalLevel.High.ToString(), workItem.Metadata[WorkItemSignalKeys.TimeCriticalitySignal]);
        Assert.Equal("short", workItem.Metadata[WorkItemSignalKeys.MessageLengthBucketSignal]);
    }

    // === Phase 1: Signal keys ===

    [Fact]
    public void WorkItemSignalKeys_TeamsScoringKeys_AreDefined()
    {
        Assert.Equal("teams.scoring.titleCues", WorkItemSignalKeys.TeamsScoringTitleCues);
        Assert.Equal("teams.scoring.bodyCues", WorkItemSignalKeys.TeamsScoringBodyCues);
        Assert.Equal("teams.scoring.mentionDetected", WorkItemSignalKeys.TeamsScoringMentionDetected);
        Assert.Equal("teams.scoring.totalScore", WorkItemSignalKeys.TeamsScoringTotalScore);
        Assert.Equal("teams.deadline.cue", WorkItemSignalKeys.TeamsDeadlineCue);
        Assert.Equal("teams.deadline.source", WorkItemSignalKeys.TeamsDeadlineSource);
    }

    // === Phase 2: Cue Arrays and Scoring Logic ===

    [Fact]
    public void TryMap_ContentScore_EmitsScoringMetadata()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "sc-01",
            Title = "URGENT: server incident",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "sev1 issue affecting production @John"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.True(workItem!.Metadata.ContainsKey(WorkItemSignalKeys.TeamsScoringTotalScore),
            "Scoring metadata should be emitted");
    }

    // === Phase 3.1: Title scoring ===

    [Fact]
    public void TryMap_StrongTitleUrgency_Weight3()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "t3-01",
            Title = "URGENT: production is down",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "Please check"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("3", workItem!.Metadata[WorkItemSignalKeys.TeamsScoringTotalScore]);
    }

    [Fact]
    public void TryMap_SingleStrongTitleCue_Weight3()
    {
        // All TitlePriorityCues are strong tokens, so any single match yields weight 3.
        // Weight 1 is reserved for non-strong title tokens in future extension.
        var message = new TeamsMessageDto
        {
            ExternalId = "t3-02",
            Title = "Review the incident report",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "No body cues here"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        // "incident" is a strong token → title weight 3; no body/mention → total = 3
        Assert.Equal("3", workItem!.Metadata[WorkItemSignalKeys.TeamsScoringTotalScore]);
    }

    [Fact]
    public void TryMap_NoTitleCues_Weight0()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "t3-03",
            Title = "Weekly status update",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "All good, nothing urgent"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("0", workItem!.Metadata[WorkItemSignalKeys.TeamsScoringTotalScore]);
    }

    // === Phase 3.2: Body scoring ===

    [Fact]
    public void TryMap_BodyHighCue_Weight3()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "b3-01",
            Title = "Quick update",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "This is sev1 incident, immediate action needed"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        // High cue (3) + no title cue (0) + no mention (0) = 3
        Assert.Equal("3", workItem!.Metadata[WorkItemSignalKeys.TeamsScoringTotalScore]);
    }

    [Fact]
    public void TryMap_BodyMediumCue_Weight1()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "b3-02",
            Title = "Status check",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "Please follow up on this request"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        // Medium cue (1) + no title cue (0) + no mention (0) = 1
        Assert.Equal("1", workItem!.Metadata[WorkItemSignalKeys.TeamsScoringTotalScore]);
    }

    [Fact]
    public void TryMap_NoBodyCues_Weight0()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "b3-03",
            Title = "Lunch menu",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "Pizza today in the cafeteria"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("0", workItem!.Metadata[WorkItemSignalKeys.TeamsScoringTotalScore]);
    }

    // === Phase 3.3: Mention detection ===

    [Fact]
    public void TryMap_MentionDetected_SetsTrue()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "m3-01",
            Title = "Review needed",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "Can you look at this @John?"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("True", workItem!.Metadata[WorkItemSignalKeys.TeamsScoringMentionDetected]);
        Assert.Equal("1", workItem.Metadata[WorkItemSignalKeys.TeamsScoringTotalScore]);
    }

    [Fact]
    public void TryMap_NoMention_SetsFalse()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "m3-02",
            Title = "General notice",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "Please review the document"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("False", workItem!.Metadata[WorkItemSignalKeys.TeamsScoringMentionDetected]);
    }

    // === Phase 3.4: Deadline detection ===

    [Fact]
    public void TryMap_DeadlineInTitle_EmitSourceTitle()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "d3-01",
            Title = "Report due Friday",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "See attached"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.True(workItem!.Metadata.ContainsKey(WorkItemSignalKeys.TeamsDeadlineCue));
        Assert.Equal("title", workItem.Metadata[WorkItemSignalKeys.TeamsDeadlineSource]);
    }

    [Fact]
    public void TryMap_DeadlineInBody_WhenTitleNoMatch()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "d3-02",
            Title = "Quick question",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "Meeting on 05/15"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.True(workItem!.Metadata.ContainsKey(WorkItemSignalKeys.TeamsDeadlineCue));
        Assert.Equal("body", workItem.Metadata[WorkItemSignalKeys.TeamsDeadlineSource]);
    }

    [Fact]
    public void TryMap_NoDeadline_SkipsMetadata()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "d3-03",
            Title = "Weekly update",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "All good here"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.False(workItem!.Metadata.ContainsKey(WorkItemSignalKeys.TeamsDeadlineCue));
        Assert.False(workItem.Metadata.ContainsKey(WorkItemSignalKeys.TeamsDeadlineSource));
    }

    [Fact]
    public void TryMap_DeadlineByEod_EmitCue()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "d3-04",
            Title = "Submit by EOD",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "No rush otherwise"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.True(workItem!.Metadata.ContainsKey(WorkItemSignalKeys.TeamsDeadlineCue));
        Assert.Equal("title", workItem.Metadata[WorkItemSignalKeys.TeamsDeadlineSource]);
    }

    // === Phase 3.5: Scoring metadata emission ===

    [Fact]
    public void TryMap_AllScoringKeysEmitted()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "sm-01",
            Title = "URGENT: sev1 incident",
            Source = "messages",
            Priority = "high",
            BodyPreview = "Production is down, immediate fix needed @Alice"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.True(workItem!.Metadata.ContainsKey(WorkItemSignalKeys.TeamsScoringTitleCues));
        Assert.True(workItem.Metadata.ContainsKey(WorkItemSignalKeys.TeamsScoringBodyCues));
        Assert.True(workItem.Metadata.ContainsKey(WorkItemSignalKeys.TeamsScoringMentionDetected));
        Assert.True(workItem.Metadata.ContainsKey(WorkItemSignalKeys.TeamsScoringTotalScore));
        Assert.Equal("True", workItem.Metadata[WorkItemSignalKeys.ActionNeededSignal]);
    }

    [Fact]
    public void TryMap_NoInputData_SkipsOptionalKeys()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "sm-02",
            Title = null,
            Source = "messages",
            Priority = "medium",
            BodyPreview = null
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        // TotalScore is always emitted when scoring runs
        Assert.True(workItem!.Metadata.ContainsKey(WorkItemSignalKeys.TeamsScoringTotalScore));
        // Title/body scoring keys should be absent when inputs are null
        Assert.False(workItem.Metadata.ContainsKey(WorkItemSignalKeys.TeamsScoringTitleCues));
        Assert.False(workItem.Metadata.ContainsKey(WorkItemSignalKeys.TeamsScoringBodyCues));
        Assert.False(workItem.Metadata.ContainsKey(WorkItemSignalKeys.TeamsScoringMentionDetected));
        // action_needed requires a cue match — no cues should mean absent
        Assert.False(workItem.Metadata.ContainsKey(WorkItemSignalKeys.ActionNeededSignal));
    }

    [Fact]
    public void TryMap_ActionNeededSet_WhenCuesDetected()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "sm-03",
            Title = "Blocker in production",
            Source = "messages",
            Priority = "medium",
            BodyPreview = "Needs attention urgently"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("True", workItem!.Metadata[WorkItemSignalKeys.ActionNeededSignal]);
    }

    // === Phase 3.6: Priority boundary ===

    [Fact]
    public void TryMap_Score7WithLowPriority_WorkItemPriorityIsLow()
    {
        var message = new TeamsMessageDto
        {
            ExternalId = "bp-01",
            Title = "URGENT: asap blocker incident",
            Source = "messages",
            Priority = "low",
            BodyPreview = "sev1 production down broken @John"
        };

        var mapped = _mapper.TryMap(message, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        // Content score should be 7 (3 title + 3 body + 1 mention)
        Assert.Equal("7", workItem!.Metadata[WorkItemSignalKeys.TeamsScoringTotalScore]);
        // But WorkItem.Priority must remain Low (from priority flag)
        Assert.Equal(WorkItemPriority.Low, workItem.Priority);
    }
}
