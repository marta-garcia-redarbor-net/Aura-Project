using Aura.Domain.WorkItems;
using Aura.Application.Models;
using Aura.Infrastructure.Adapters.Connectors.Outlook;

namespace Aura.UnitTests.Ingestion.Outlook;

public class OutlookWorkItemMapperTests
{
    private readonly OutlookWorkItemMapper _mapper = new();

    [Fact]
    public void TryMap_ValidPayload_MapsCanonicalWorkItem()
    {
        var dto = CreateBaseDto();

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("mail-1", workItem!.ExternalId);
        Assert.Equal("Incident needs attention", workItem.Title);
        Assert.Equal("inbox", workItem.Source);
        Assert.Equal(WorkItemSourceType.OutlookEmail, workItem.SourceType);
    }

    [Fact]
    public void TryMap_MissingExternalId_SkipsItem()
    {
        var dto = CreateBaseDto() with { ExternalId = null };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.False(mapped);
        Assert.Null(workItem);
    }

    [Fact]
    public void TryMap_MissingSubject_DefaultsTitle_AndRecordsMetadata()
    {
        var dto = CreateBaseDto() with { Subject = null };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("Outlook email mail-1", workItem!.Title);
        Assert.Equal("absent", workItem.Metadata["outlook.subject.raw"]);
        Assert.Equal("defaulted", workItem.Metadata["outlook.subject.resolution"]);
    }

    [Fact]
    public void TryMap_ImportanceHigh_MapsHighPriority()
    {
        var dto = CreateBaseDto() with { Importance = "High", Subject = "status update", SenderAddress = "unknown@aura.dev", BodyPreview = "normal body" };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.Equal(WorkItemPriority.High, workItem!.Priority);
    }

    [Fact]
    public void TryMap_ImportanceNormalOnly_MapsMediumPriority()
    {
        var dto = CreateBaseDto() with { Importance = "Normal", Subject = "status update", SenderAddress = "unknown@aura.dev", BodyPreview = "normal body" };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.Equal(WorkItemPriority.Medium, workItem!.Priority);
    }

    [Fact]
    public void TryMap_ImportanceLowOnly_MapsLowPriority()
    {
        var dto = CreateBaseDto() with { Importance = "Low", Subject = "status update", SenderAddress = "unknown@aura.dev", BodyPreview = "normal body" };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.Equal(WorkItemPriority.Low, workItem!.Priority);
    }

    [Fact]
    public void TryMap_AbsentImportanceWithStrongSender_MapsHighPriority()
    {
        var dto = CreateBaseDto() with { Importance = null, Subject = "status update", SenderAddress = "ceo@aura.dev", BodyPreview = "normal body" };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.Equal(WorkItemPriority.High, workItem!.Priority);
    }

    [Fact]
    public void TryMap_AbsentImportanceWithBodyCue_MapsHighPriority()
    {
        var dto = CreateBaseDto() with { Importance = null, Subject = "status update", SenderAddress = "unknown@aura.dev", BodyPreview = "production down immediate action required" };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.Equal(WorkItemPriority.High, workItem!.Priority);
    }

    [Fact]
    public void TryMap_AllSignalsAbsent_MapsMediumPriority()
    {
        var dto = CreateBaseDto() with { Importance = null, Subject = "weekly recap", SenderAddress = "unknown@aura.dev", BodyPreview = "regular note" };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.Equal(WorkItemPriority.Medium, workItem!.Priority);
    }

    [Fact]
    public void TryMap_MaxSignals_MapsCriticalPriority()
    {
        var dto = CreateBaseDto() with { Importance = "High", Subject = "urgent escalation", SenderAddress = "ceo@aura.dev", BodyPreview = "production down immediate" };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.Equal(WorkItemPriority.Critical, workItem!.Priority);
    }

    [Fact]
    public void TryMap_AlwaysWritesScoringMetadataKeys()
    {
        var dto = CreateBaseDto() with { Importance = null, Subject = "weekly recap", SenderAddress = "unknown@aura.dev", BodyPreview = "regular note" };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Contains("outlook.importance.raw", workItem!.Metadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("outlook.scoring.subjectCues", workItem.Metadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("outlook.scoring.senderWeight", workItem.Metadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("outlook.scoring.bodyCues", workItem.Metadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("outlook.scoring.totalScore", workItem.Metadata.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryMap_SubjectDeadlineCue_WritesDeadlineMetadata()
    {
        var dto = CreateBaseDto() with { Subject = "Need report by EOD for finance leadership sync" };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("subject", workItem!.Metadata["outlook.deadline.source"]);
        Assert.Contains("by eod", workItem.Metadata["outlook.deadline.cue"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("finance leadership sync", workItem.Metadata["outlook.deadline.cue"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryMap_UnrecognizedImportance_WithSenderSignal_UsesRemainingSignals()
    {
        var dto = CreateBaseDto() with
        {
            Importance = "CriticalNow",
            Subject = "status update",
            SenderAddress = "ceo@aura.dev",
            BodyPreview = "regular note"
        };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal(WorkItemPriority.High, workItem!.Priority);
        Assert.Equal("CriticalNow", workItem.Metadata["outlook.importance.raw"]);
    }

    [Fact]
    public void TryMap_SubjectWithoutDeadline_BodyDeadlineCue_FallsBackToBodyWithContextExcerpt()
    {
        var dto = CreateBaseDto() with
        {
            Subject = "Weekly recap",
            BodyPreview = "Please send the revised roadmap by end of day with dependencies and risk owners."
        };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("body", workItem!.Metadata["outlook.deadline.source"]);
        Assert.Contains("by end of day", workItem.Metadata["outlook.deadline.cue"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dependencies and risk owners", workItem.Metadata["outlook.deadline.cue"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryMap_DeadlineDatePattern_StoresContextExcerptInsteadOfDateTokenOnly()
    {
        var dto = CreateBaseDto() with
        {
            Subject = "Status update",
            BodyPreview = "Please complete deployment checklist by 07/15 before release readiness review."
        };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("body", workItem!.Metadata["outlook.deadline.source"]);
        Assert.Contains("07/15", workItem.Metadata["outlook.deadline.cue"], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("release readiness review", workItem.Metadata["outlook.deadline.cue"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryMap_NoDeadlineCue_DoesNotWriteDeadlineMetadata()
    {
        var dto = CreateBaseDto() with { Subject = "Status update", BodyPreview = "regular info" };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.DoesNotContain("outlook.deadline.source", workItem!.Metadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("outlook.deadline.cue", workItem.Metadata.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryMap_WritesCanonicalTriageMetadata()
    {
        var dto = CreateBaseDto() with
        {
            Importance = "High",
            SenderAddress = "ceo@aura.dev",
            BodyPreview = "Please review this urgent incident update"
        };

        var mapped = _mapper.TryMap(dto, out var workItem);

        Assert.True(mapped);
        Assert.NotNull(workItem);
        Assert.Equal("ceo@aura.dev", workItem!.Metadata[WorkItemSignalKeys.CanonicalSender]);
        Assert.Equal("Please review this urgent incident update", workItem.Metadata[WorkItemSignalKeys.CanonicalSnippet]);
        Assert.Equal("True", workItem.Metadata[WorkItemSignalKeys.ActionNeededSignal]);
        Assert.Equal(SignalLevel.Critical.ToString(), workItem.Metadata[WorkItemSignalKeys.TimeCriticalitySignal]);
        Assert.Equal("short", workItem.Metadata[WorkItemSignalKeys.MessageLengthBucketSignal]);
    }

    private static OutlookEmailDto CreateBaseDto() =>
        new()
        {
            ExternalId = "mail-1",
            Subject = "Incident needs attention",
            Importance = "Normal",
            SenderAddress = "manager@aura.dev",
            BodyPreview = "please review this",
            ReceivedDateTime = new DateTimeOffset(2026, 06, 21, 10, 0, 0, TimeSpan.Zero),
            CorrelationId = "corr-1",
            ConversationId = "conv-1"
        };
}
