using Aura.Application.Models;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Options;
using Aura.Infrastructure.Adapters.Services.Rules;
using Microsoft.Extensions.Options;

namespace Aura.UnitTests.Services.Rules;

public class DeadlineUrgencyRuleTests
{
    private static WorkItem CreateWorkItemWithDeadline(DateTimeOffset deadline)
    {
        var metadata = new Dictionary<string, string>
        {
            ["outlook.deadline.response"] = deadline.ToString("O")
        };
        return new WorkItem("ext-1", "Meeting Response Needed", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.High, metadata);
    }

    private static WorkItem CreateWorkItemWithoutDeadline()
    {
        return new WorkItem("ext-1", "General Email", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium,
            new Dictionary<string, string>());
    }

    [Fact]
    public async Task EvaluateAsync_DeadlineWithinWindow_ReturnsMatched()
    {
        var options = Options.Create(new InterruptionOptions { DeadlineWindowHours = 2 });
        var rule = new DeadlineUrgencyRule(options);
        var deadline = DateTimeOffset.UtcNow.AddHours(1);
        var item = CreateWorkItemWithDeadline(deadline);
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.True(result.Matched);
        Assert.Equal("DeadlineUrgencyRule", result.RuleName);
    }

    [Fact]
    public async Task EvaluateAsync_DeadlineOutsideWindow_ReturnsNotMatched()
    {
        var options = Options.Create(new InterruptionOptions { DeadlineWindowHours = 2 });
        var rule = new DeadlineUrgencyRule(options);
        var deadline = DateTimeOffset.UtcNow.AddDays(3);
        var item = CreateWorkItemWithDeadline(deadline);
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.False(result.Matched);
    }

    [Fact]
    public async Task EvaluateAsync_NoDeadlineMetadata_ReturnsNotMatched()
    {
        var options = Options.Create(new InterruptionOptions { DeadlineWindowHours = 2 });
        var rule = new DeadlineUrgencyRule(options);
        var item = CreateWorkItemWithoutDeadline();
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.False(result.Matched);
    }

    [Fact]
    public void Priority_ReturnsForty()
    {
        var options = Options.Create(new InterruptionOptions());
        var rule = new DeadlineUrgencyRule(options);

        Assert.Equal(40, rule.Priority);
    }
}
