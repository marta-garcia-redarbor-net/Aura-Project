using Aura.Domain.WorkItems;

namespace Aura.UnitTests.WorkItems;

public class WorkItemTests
{
    private static WorkItem CreateValidWorkItem(
        string externalId = "ext-123",
        string title = "Review PR #42",
        string source = "github",
        WorkItemSourceType sourceType = WorkItemSourceType.PrReview,
        WorkItemPriority priority = WorkItemPriority.High,
        IReadOnlyDictionary<string, string>? metadata = null,
        string? correlationId = "corr-123",
        DateTimeOffset? capturedAtUtc = null)
    {
        metadata ??= new Dictionary<string, string>
        {
            ["origin"] = "unit-test"
        };

        return new WorkItem(
            externalId,
            title,
            source,
            sourceType,
            priority,
            metadata,
            correlationId,
            capturedAtUtc ?? new DateTimeOffset(2026, 01, 01, 12, 00, 00, TimeSpan.Zero));
    }

    [Fact]
    public void NewWorkItem_HasPendingStatus()
    {
        var item = CreateValidWorkItem();

        Assert.Equal(WorkItemStatus.Pending, item.Status);
    }

    [Fact]
    public void NewWorkItem_SetsProperties()
    {
        var capturedAt = new DateTimeOffset(2026, 01, 01, 12, 00, 00, TimeSpan.Zero);
        var metadata = new Dictionary<string, string>
        {
            ["channel"] = "pull-request"
        };
        var item = new WorkItem(
            "ext-123",
            "Review PR #42",
            "github",
            WorkItemSourceType.PrReview,
            WorkItemPriority.High,
            metadata,
            "corr-123",
            capturedAt);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal("ext-123", item.ExternalId);
        Assert.Equal("Review PR #42", item.Title);
        Assert.Equal("github", item.Source);
        Assert.Equal(WorkItemSourceType.PrReview, item.SourceType);
        Assert.Equal(WorkItemPriority.High, item.Priority);
        Assert.Equal(metadata, item.Metadata);
        Assert.Equal("corr-123", item.CorrelationId);
        Assert.Equal(capturedAt, item.CapturedAtUtc);
        Assert.Equal("v1", item.SchemaVersion);
        Assert.Null(item.FaultReason);
        Assert.True(item.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void MarkProcessing_FromPending_Succeeds()
    {
        var item = CreateValidWorkItem();

        item.MarkProcessing();

        Assert.Equal(WorkItemStatus.Processing, item.Status);
    }

    [Fact]
    public void MarkCompleted_FromProcessing_Succeeds()
    {
        var item = CreateValidWorkItem();
        item.MarkProcessing();

        item.MarkCompleted();

        Assert.Equal(WorkItemStatus.Completed, item.Status);
    }

    [Fact]
    public void MarkFaulted_FromProcessing_SetsStatusAndReason()
    {
        var item = CreateValidWorkItem();
        item.MarkProcessing();

        item.MarkFaulted("plugin crashed");

        Assert.Equal(WorkItemStatus.Faulted, item.Status);
        Assert.Equal("plugin crashed", item.FaultReason);
    }

    [Fact]
    public void MarkCompleted_FromPending_Throws()
    {
        var item = CreateValidWorkItem();

        Assert.Throws<InvalidOperationException>(() => item.MarkCompleted());
    }

    [Fact]
    public void MarkProcessing_FromCompleted_Throws()
    {
        var item = CreateValidWorkItem();
        item.MarkProcessing();
        item.MarkCompleted();

        Assert.Throws<InvalidOperationException>(() => item.MarkProcessing());
    }

    [Fact]
    public void MarkFaulted_FromPending_Throws()
    {
        var item = CreateValidWorkItem();

        Assert.Throws<InvalidOperationException>(() => item.MarkFaulted("reason"));
    }

    [Fact]
    public void MarkCompleted_FromFaulted_Throws()
    {
        var item = CreateValidWorkItem();
        item.MarkProcessing();
        item.MarkFaulted("broke");

        Assert.Throws<InvalidOperationException>(() => item.MarkCompleted());
    }

    [Fact]
    public void MarkFaulted_FromCompleted_Throws()
    {
        var item = CreateValidWorkItem();
        item.MarkProcessing();
        item.MarkCompleted();

        Assert.Throws<InvalidOperationException>(() => item.MarkFaulted("too late"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_EmptyExternalId_ThrowsArgumentException(string? externalId)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            CreateValidWorkItem(externalId: externalId!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_EmptyTitle_ThrowsArgumentException(string? title)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            CreateValidWorkItem(title: title!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_EmptySource_ThrowsArgumentException(string? source)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            CreateValidWorkItem(source: source!));
    }

    [Fact]
    public void Constructor_InvalidSourceType_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            CreateValidWorkItem(sourceType: (WorkItemSourceType)999));
    }

    [Fact]
    public void Constructor_InvalidPriority_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            CreateValidWorkItem(priority: (WorkItemPriority)999));
    }

    [Fact]
    public void Constructor_NullMetadata_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new WorkItem(
                "ext-123",
                "Review PR #42",
                "github",
                WorkItemSourceType.PrReview,
                WorkItemPriority.High,
                null!,
                "corr-123",
                new DateTimeOffset(2026, 01, 01, 12, 00, 00, TimeSpan.Zero)));
    }

    [Fact]
    public void MarkAutoCompleted_FromPending_TransitionsToCompleted()
    {
        var item = CreateValidWorkItem();

        item.MarkAutoCompleted();

        Assert.Equal(WorkItemStatus.Completed, item.Status);
    }

    [Fact]
    public void MarkAutoCompleted_FromProcessing_ThrowsInvalidOperationException()
    {
        var item = CreateValidWorkItem();
        item.MarkProcessing();

        Assert.Throws<InvalidOperationException>(() => item.MarkAutoCompleted());
    }

    [Fact]
    public void MarkAutoCompleted_FromCompleted_ThrowsInvalidOperationException()
    {
        var item = CreateValidWorkItem();
        item.MarkProcessing();
        item.MarkCompleted();

        Assert.Throws<InvalidOperationException>(() => item.MarkAutoCompleted());
    }

    [Fact]
    public void MarkAutoCompleted_FromFaulted_ThrowsInvalidOperationException()
    {
        var item = CreateValidWorkItem();
        item.MarkProcessing();
        item.MarkFaulted("reason");

        Assert.Throws<InvalidOperationException>(() => item.MarkAutoCompleted());
    }

    [Fact]
    public void WorkItemSourceType_TeamsChat_HasValue14()
    {
        Assert.Equal(14, (int)WorkItemSourceType.TeamsChat);
    }

    [Fact]
    public void Constructor_EmptyMetadata_IsAccepted()
    {
        var item = CreateValidWorkItem(metadata: new Dictionary<string, string>());

        Assert.Empty(item.Metadata);
    }

    [Fact]
    public void Constructor_PopulatedMetadata_IsAcceptedAndPreserved()
    {
        var metadata = new Dictionary<string, string>
        {
            ["key"] = "value",
            ["channel"] = "teams"
        };

        var item = CreateValidWorkItem(metadata: metadata);

        Assert.Equal(2, item.Metadata.Count);
        Assert.Equal("value", item.Metadata["key"]);
        Assert.Equal("teams", item.Metadata["channel"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyCorrelationId_GeneratesCorrelationId(string? correlationId)
    {
        var item = CreateValidWorkItem(correlationId: correlationId);

        Assert.False(string.IsNullOrWhiteSpace(item.CorrelationId));
    }

    [Fact]
    public void Constructor_CallerProvidedCorrelationId_IsPreserved()
    {
        var item = CreateValidWorkItem(correlationId: "corr-explicit-42");

        Assert.Equal("corr-explicit-42", item.CorrelationId);
    }

    [Fact]
    public void Constructor_CapturedAtUtcProvided_IsPreserved()
    {
        var expected = new DateTimeOffset(2025, 12, 31, 20, 00, 00, TimeSpan.Zero);

        var item = CreateValidWorkItem(capturedAtUtc: expected);

        Assert.Equal(expected, item.CapturedAtUtc);
    }

    [Fact]
    public void Constructor_CapturedAtUtcMissing_FallsBackToCurrentUtc()
    {
        var before = DateTimeOffset.UtcNow;
        var item = new WorkItem(
            "ext-123",
            "Review PR #42",
            "github",
            WorkItemSourceType.PrReview,
            WorkItemPriority.High,
            new Dictionary<string, string>(),
            "corr-123",
            capturedAtUtc: null);
        var after = DateTimeOffset.UtcNow;

        Assert.True(item.CapturedAtUtc >= before);
        Assert.True(item.CapturedAtUtc <= after);
    }

    [Fact]
    public void Constructor_CapturedAtUtcMinValue_FallsBackToCurrentUtc()
    {
        var before = DateTimeOffset.UtcNow;
        var item = CreateValidWorkItem(capturedAtUtc: DateTimeOffset.MinValue);
        var after = DateTimeOffset.UtcNow;

        Assert.NotEqual(DateTimeOffset.MinValue, item.CapturedAtUtc);
        Assert.True(item.CapturedAtUtc >= before);
        Assert.True(item.CapturedAtUtc <= after);
    }

    [Fact]
    public void Constructor_CapturedAtUtcWithLocalOffset_IsAcceptedAndPreserved()
    {
        var localOffset = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 01, 01));
        var expected = new DateTimeOffset(2026, 01, 01, 09, 30, 00, localOffset);

        var item = CreateValidWorkItem(capturedAtUtc: expected);

        Assert.Equal(expected, item.CapturedAtUtc);
    }

    [Fact]
    public void Constructor_SchemaVersion_IsAlwaysV1()
    {
        var item = CreateValidWorkItem();

        Assert.Equal("v1", item.SchemaVersion);
    }

    // ============================================================
    // Phase: W3-H3 — PriorityScore
    // ============================================================

    [Fact]
    public void Constructor_WithoutPriorityScore_DefaultsToNull()
    {
        var item = CreateValidWorkItem();

        Assert.Null(item.PriorityScore);
    }

    [Fact]
    public void Constructor_WithExplicitPriorityScore_PreservesValue()
    {
        var item = new WorkItem(
            "ext-ps-1",
            "Priority Item",
            "inbox",
            WorkItemSourceType.OutlookEmail,
            WorkItemPriority.High,
            new Dictionary<string, string>(),
            null,
            null,
            priorityScore: 85);

        Assert.Equal(85, item.PriorityScore);
    }

    [Fact]
    public void PriorityScore_CanBeNull_AfterConstruction()
    {
        var item = new WorkItem(
            "ext-ps-2",
            "Null Score Item",
            "inbox",
            WorkItemSourceType.OutlookEmail,
            WorkItemPriority.Medium,
            new Dictionary<string, string>(),
            null,
            null,
            priorityScore: null);

        Assert.Null(item.PriorityScore);
    }

    [Fact]
    public void Constructor_WithZeroPriorityScore_PreservesZero()
    {
        var item = new WorkItem(
            "ext-ps-zero",
            "Zero Score",
            "inbox",
            WorkItemSourceType.OutlookEmail,
            WorkItemPriority.Low,
            new Dictionary<string, string>(),
            null,
            null,
            priorityScore: 0);

        Assert.Equal(0, item.PriorityScore);
    }

    [Fact]
    public void PriorityScore_DoesNotAffectStatusTransitions()
    {
        var item = new WorkItem(
            "ext-ps-trans",
            "Transitions",
            "inbox",
            WorkItemSourceType.OutlookEmail,
            WorkItemPriority.High,
            new Dictionary<string, string>(),
            null,
            null,
            priorityScore: 100);

        Assert.Equal(WorkItemStatus.Pending, item.Status);
        Assert.Equal(100, item.PriorityScore);

        item.MarkProcessing();
        Assert.Equal(WorkItemStatus.Processing, item.Status);
        Assert.Equal(100, item.PriorityScore); // unchanged

        item.MarkCompleted();
        Assert.Equal(WorkItemStatus.Completed, item.Status);
        Assert.Equal(100, item.PriorityScore); // unchanged
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MarkFaulted_EmptyReason_Throws(string? reason)
    {
        var item = CreateValidWorkItem();
        item.MarkProcessing();

        Assert.ThrowsAny<ArgumentException>(() => item.MarkFaulted(reason!));
    }
}
