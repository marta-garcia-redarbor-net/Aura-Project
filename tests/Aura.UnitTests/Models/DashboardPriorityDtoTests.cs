using Aura.Application.Models;

namespace Aura.UnitTests.Models;

public class DashboardPriorityDtoTests
{
    [Fact]
    public void Default_AllCountsZero()
    {
        var dto = new DashboardPriorityDto();

        Assert.Equal(0, dto.CriticalCount);
        Assert.Equal(0, dto.HighCount);
        Assert.Equal(0, dto.MediumCount);
        Assert.Equal(0, dto.LowCount);
        Assert.Empty(dto.TopItems);
    }

    [Fact]
    public void CanSetCounts()
    {
        var dto = new DashboardPriorityDto
        {
            CriticalCount = 2,
            HighCount = 5,
            MediumCount = 10,
            LowCount = 3
        };

        Assert.Equal(2, dto.CriticalCount);
        Assert.Equal(5, dto.HighCount);
        Assert.Equal(10, dto.MediumCount);
        Assert.Equal(3, dto.LowCount);
    }

    [Fact]
    public void CanSetTopItems()
    {
        var items = new[]
        {
            new InboxItemPreviewDto("A", "email", "2m ago", 95.0, "review"),
            new InboxItemPreviewDto("B", "chat", "5m ago", 80.0, "reply"),
            new InboxItemPreviewDto("C", "pr-review", "10m ago", 70.0, "merge")
        };

        var dto = new DashboardPriorityDto
        {
            TopItems = items
        };

        Assert.Equal(3, dto.TopItems.Count);
        Assert.Equal("A", dto.TopItems[0].Title);
    }
}
