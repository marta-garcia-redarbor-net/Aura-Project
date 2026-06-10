using Aura.Domain.WorkItems;

namespace Aura.UnitTests.WorkItems;

public class WorkItemTests
{
    [Fact]
    public void NewWorkItem_HasPendingStatus()
    {
        var item = new WorkItem("Test title", "manual");

        Assert.Equal(WorkItemStatus.Pending, item.Status);
    }

    [Fact]
    public void NewWorkItem_SetsProperties()
    {
        var item = new WorkItem("Review PR #42", "github");

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal("Review PR #42", item.Title);
        Assert.Equal("github", item.Source);
        Assert.Null(item.FaultReason);
        Assert.True(item.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void MarkProcessing_FromPending_Succeeds()
    {
        var item = new WorkItem("Task", "manual");

        item.MarkProcessing();

        Assert.Equal(WorkItemStatus.Processing, item.Status);
    }

    [Fact]
    public void MarkCompleted_FromProcessing_Succeeds()
    {
        var item = new WorkItem("Task", "manual");
        item.MarkProcessing();

        item.MarkCompleted();

        Assert.Equal(WorkItemStatus.Completed, item.Status);
    }

    [Fact]
    public void MarkFaulted_FromProcessing_SetsStatusAndReason()
    {
        var item = new WorkItem("Task", "manual");
        item.MarkProcessing();

        item.MarkFaulted("plugin crashed");

        Assert.Equal(WorkItemStatus.Faulted, item.Status);
        Assert.Equal("plugin crashed", item.FaultReason);
    }

    [Fact]
    public void MarkCompleted_FromPending_Throws()
    {
        var item = new WorkItem("Task", "manual");

        Assert.Throws<InvalidOperationException>(() => item.MarkCompleted());
    }

    [Fact]
    public void MarkProcessing_FromCompleted_Throws()
    {
        var item = new WorkItem("Task", "manual");
        item.MarkProcessing();
        item.MarkCompleted();

        Assert.Throws<InvalidOperationException>(() => item.MarkProcessing());
    }

    [Fact]
    public void MarkFaulted_FromPending_Throws()
    {
        var item = new WorkItem("Task", "manual");

        Assert.Throws<InvalidOperationException>(() => item.MarkFaulted("reason"));
    }

    [Fact]
    public void MarkCompleted_FromFaulted_Throws()
    {
        var item = new WorkItem("Task", "manual");
        item.MarkProcessing();
        item.MarkFaulted("broke");

        Assert.Throws<InvalidOperationException>(() => item.MarkCompleted());
    }

    [Fact]
    public void MarkFaulted_FromCompleted_Throws()
    {
        var item = new WorkItem("Task", "manual");
        item.MarkProcessing();
        item.MarkCompleted();

        Assert.Throws<InvalidOperationException>(() => item.MarkFaulted("too late"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Constructor_EmptyTitle_Throws(string? title)
    {
        Assert.ThrowsAny<ArgumentException>(() => new WorkItem(title!, "manual"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Constructor_EmptySource_Throws(string? source)
    {
        Assert.ThrowsAny<ArgumentException>(() => new WorkItem("Title", source!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MarkFaulted_EmptyReason_Throws(string? reason)
    {
        var item = new WorkItem("Task", "manual");
        item.MarkProcessing();

        Assert.ThrowsAny<ArgumentException>(() => item.MarkFaulted(reason!));
    }
}
