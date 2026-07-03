using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Services;
using NSubstitute;

namespace Aura.UnitTests.Services;

public class InterruptionPolicyEngineTests
{
    private sealed class StubRule : IInterruptionRule
    {
        public string Name { get; }
        public int Priority { get; }
        public RuleResult Result { get; }

        public StubRule(string name, int priority, RuleResult result)
        {
            Name = name;
            Priority = priority;
            Result = result;
        }

        public Task<RuleResult> EvaluateAsync(EvaluationContext context, CancellationToken ct)
            => Task.FromResult(Result);
    }

    private static RuleResult CreateResult(string ruleName, bool matched, double score = 0)
        => new(ruleName, matched, score, 1.0, matched ? $"{ruleName} triggered" : null);

    private static WorkItem CreateWorkItem() =>
        new("ext-1", "Test Item", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium,
            new Dictionary<string, string>());

    [Fact]
    public async Task EvaluateAsync_FirstRuleInterruptNow_ShortCircuitsAndReturnsInterruptNow()
    {
        var rule1 = new StubRule("Rule1", 10, CreateResult("Rule1", true));
        var rule2 = new StubRule("Rule2", 20, CreateResult("Rule2", false));
        var engine = new InterruptionPolicyEngine(new[] { rule1, rule2 });
        var item = CreateWorkItem();

        var verdict = await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Equal(InterruptionDecision.InterruptNow, verdict.Decision);
        Assert.Equal("Rule1", verdict.TriggerRule);
    }

    [Fact]
    public async Task EvaluateAsync_NoMatch_ReturnsQueueVerdict()
    {
        var rule1 = new StubRule("Rule1", 10, CreateResult("Rule1", false));
        var rule2 = new StubRule("Rule2", 20, CreateResult("Rule2", false));
        var engine = new InterruptionPolicyEngine(new[] { rule1, rule2 });
        var item = CreateWorkItem();

        var verdict = await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Equal(InterruptionDecision.Queue, verdict.Decision);
        Assert.Null(verdict.TriggerRule);
    }

    [Fact]
    public async Task EvaluateAsync_ReportContainsAllRuleResults()
    {
        var rule1 = new StubRule("Rule1", 10, CreateResult("Rule1", true));
        var rule2 = new StubRule("Rule2", 20, CreateResult("Rule2", false));
        var engine = new InterruptionPolicyEngine(new[] { rule1, rule2 });
        var item = CreateWorkItem();

        var verdict = await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Equal(2, verdict.Report.Results.Count);
        Assert.Contains(verdict.Report.Results, r => r.RuleName == "Rule1" && r.Matched);
        Assert.Contains(verdict.Report.Results, r => r.RuleName == "Rule2" && !r.Matched);
    }

    [Fact]
    public async Task EvaluateAsync_AllRulesRunEvenAfterInterruptNow_ForReport()
    {
        var rule1 = new StubRule("Rule1", 10, CreateResult("Rule1", true));
        var rule2 = new StubRule("Rule2", 20, CreateResult("Rule2", true));
        var engine = new InterruptionPolicyEngine(new[] { rule1, rule2 });
        var item = CreateWorkItem();

        var verdict = await engine.EvaluateAsync(item, CancellationToken.None);

        // Verdict says InterruptNow from Rule1
        Assert.Equal(InterruptionDecision.InterruptNow, verdict.Decision);
        Assert.Equal("Rule1", verdict.TriggerRule);

        // But ALL rules still ran for the report
        Assert.Equal(2, verdict.Report.Results.Count);
        Assert.Contains(verdict.Report.Results, r => r.RuleName == "Rule2" && r.Matched);
    }

    [Fact]
    public async Task EvaluateAsync_NoRulesRegistered_ReturnsQueueWithEmptyReport()
    {
        var engine = new InterruptionPolicyEngine(Array.Empty<IInterruptionRule>());
        var item = CreateWorkItem();

        var verdict = await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Equal(InterruptionDecision.Queue, verdict.Decision);
        Assert.Empty(verdict.Report.Results);
    }
}
