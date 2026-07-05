using Aura.Domain.WorkItems;

namespace Aura.UnitTests.WorkItems;

public class WorkItemPriorityTests
{
    [Fact]
    public void GetDefaultScore_Critical_Returns100()
    {
        var score = WorkItemPriority.Critical.GetDefaultScore();

        Assert.Equal(100, score);
    }

    [Fact]
    public void GetDefaultScore_High_Returns75()
    {
        var score = WorkItemPriority.High.GetDefaultScore();

        Assert.Equal(75, score);
    }

    [Fact]
    public void GetDefaultScore_Medium_Returns50()
    {
        var score = WorkItemPriority.Medium.GetDefaultScore();

        Assert.Equal(50, score);
    }

    [Fact]
    public void GetDefaultScore_Low_Returns25()
    {
        var score = WorkItemPriority.Low.GetDefaultScore();

        Assert.Equal(25, score);
    }
}
