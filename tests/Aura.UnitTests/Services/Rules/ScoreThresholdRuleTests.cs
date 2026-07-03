using Aura.Application.Models;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Options;
using Aura.Infrastructure.Adapters.Services.Rules;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Aura.UnitTests.Services.Rules;

public class ScoreThresholdRuleTests
{
    private static WorkItem CreateWorkItem(string title, double score)
    {
        var metadata = new Dictionary<string, string>
        {
            // Use invariant culture so the rule's NumberStyles.Any | InvariantCulture parse works
            ["outlook.scoring.total"] = score.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
        };
        return new WorkItem("ext-1", title, "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium, metadata);
    }

    [Fact]
    public async Task EvaluateAsync_ScoreAboveThreshold_ReturnsMatched()
    {
        var options = Options.Create(new InterruptionOptions { UrgentThreshold = 6.0 });
        var rule = new ScoreThresholdRule(options);
        var item = CreateWorkItem("Urgent Email", 8.5);
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.True(result.Matched);
        Assert.Equal("ScoreThresholdRule", result.RuleName);
        Assert.Equal(8.5, result.Score);
    }

    [Fact]
    public async Task EvaluateAsync_ScoreBelowThreshold_ReturnsNotMatched()
    {
        var options = Options.Create(new InterruptionOptions { UrgentThreshold = 6.0 });
        var rule = new ScoreThresholdRule(options);
        var item = CreateWorkItem("Normal Email", 3.0);
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.False(result.Matched);
        Assert.Equal(3.0, result.Score);
    }

    [Fact]
    public async Task EvaluateAsync_ThresholdFromOptions_Respected()
    {
        var options = Options.Create(new InterruptionOptions { UrgentThreshold = 9.0 });
        var rule = new ScoreThresholdRule(options);
        var item = CreateWorkItem("Moderate Email", 8.5);
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.False(result.Matched);
    }

    [Fact]
    public void Priority_ReturnsTen()
    {
        var options = Options.Create(new InterruptionOptions());
        var rule = new ScoreThresholdRule(options);

        Assert.Equal(10, rule.Priority);
    }
}
