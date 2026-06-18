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
    [InlineData(null)]
    public void Constructor_EmptyExternalId_ThrowsArgumentException(string? externalId)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            CreateValidWorkItem(externalId: externalId!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Constructor_EmptyTitle_ThrowsArgumentException(string? title)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            CreateValidWorkItem(title: title!));
    }

    [Theory]
    [InlineData("")]
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
    public void Constructor_EmptyMetadata_IsAccepted()
    {
        var item = CreateValidWorkItem(metadata: new Dictionary<string, string>());

        Assert.Empty(item.Metadata);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
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
    public void Constructor_SchemaVersion_IsAlwaysV1()
    {
        var item = CreateValidWorkItem();

        Assert.Equal("v1", item.SchemaVersion);
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
