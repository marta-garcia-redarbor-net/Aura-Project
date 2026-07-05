using Aura.Application.Models;

namespace Aura.UnitTests.Models;

public class WorkItemDetailDtoTests
{
    [Fact]
    public void Dto_HasPriorityScore_WhenProvided()
    {
        var dto = new WorkItemDetailDto(
            Guid.NewGuid(), "ext-1", "Test", "inbox",
            "OutlookEmail", "Pending", "High", "1m ago",
            DateTimeOffset.UtcNow)
        {
            PriorityScore = 80
        };

        Assert.Equal(80, dto.PriorityScore);
    }

    [Fact]
    public void Dto_PriorityScore_DefaultsToNull()
    {
        var dto = new WorkItemDetailDto(
            Guid.NewGuid(), "ext-1", "Test", "inbox",
            "OutlookEmail", "Pending", "High", "1m ago",
            DateTimeOffset.UtcNow);

        Assert.Null(dto.PriorityScore);
    }
}

public class InboxItemPreviewDtoTests
{
    [Fact]
    public void Dto_HasPriorityScore_WhenProvided()
    {
        var dto = new InboxItemPreviewDto(
            "Title", "inbox", "1m ago", 0.5, "Review")
        {
            PriorityScore = 85
        };

        Assert.Equal(85, dto.PriorityScore);
    }

    [Fact]
    public void Dto_PriorityScore_DefaultsToNull()
    {
        var dto = new InboxItemPreviewDto(
            "Title", "inbox", "1m ago", 0.5, "Review");

        Assert.Null(dto.PriorityScore);
    }
}
