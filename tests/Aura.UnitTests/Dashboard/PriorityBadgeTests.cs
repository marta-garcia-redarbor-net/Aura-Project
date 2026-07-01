using Aura.UI.Components.Dashboard;
using Bunit;

namespace Aura.UnitTests.Dashboard;

public class PriorityBadgeTests : TestContext
{
    [Theory]
    [InlineData("Critical", "priority-badge--critical")]
    [InlineData("High", "priority-badge--high")]
    [InlineData("Medium", "priority-badge--medium")]
    [InlineData("Low", "priority-badge--low")]
    public void PriorityBadge_RendersCorrectCssClass(string level, string expectedClass)
    {
        var cut = RenderComponent<PriorityBadge>(parameters => parameters
            .Add(p => p.Level, level));

        var badge = cut.Find("[data-testid=\"priority-badge\"]");
        Assert.Contains(expectedClass, badge.ClassName);
    }

    [Theory]
    [InlineData("Critical")]
    [InlineData("High")]
    [InlineData("Medium")]
    [InlineData("Low")]
    public void PriorityBadge_RendersLevelText(string level)
    {
        var cut = RenderComponent<PriorityBadge>(parameters => parameters
            .Add(p => p.Level, level));

        Assert.Contains(level, cut.Markup);
    }

    [Fact]
    public void PriorityBadge_IncludesDotElement()
    {
        var cut = RenderComponent<PriorityBadge>(parameters => parameters
            .Add(p => p.Level, "High"));

        Assert.NotNull(cut.Find("[data-testid=\"priority-badge-dot\"]"));
    }
}
