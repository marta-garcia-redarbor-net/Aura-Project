using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Services.Rules;
using NSubstitute;

namespace Aura.UnitTests.Services.Rules;

public class KeywordMatchRuleTests
{
    private static WorkItem CreateWorkItem(string subject, string? body = null)
    {
        var metadata = new Dictionary<string, string>
        {
            ["outlook.subject"] = subject
        };
        if (body is not null)
        {
            metadata["outlook.body"] = body;
        }
        return new WorkItem("ext-1", subject, "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium, metadata);
    }

    [Fact]
    public async Task EvaluateAsync_SubjectMatchesKeyword_CaseInsensitive_ReturnsMatched()
    {
        var store = Substitute.For<IAlertRuleStore>();
        store.GetKeywordsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string> { "urgent", "security" });
        var rule = new KeywordMatchRule(store);
        var item = CreateWorkItem("URGENT: Server Down");
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.True(result.Matched);
        Assert.Equal("KeywordMatchRule", result.RuleName);
    }

    [Fact]
    public async Task EvaluateAsync_BodyMatchesKeyword_ReturnsMatched()
    {
        var store = Substitute.For<IAlertRuleStore>();
        store.GetKeywordsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string> { "critical" });
        var rule = new KeywordMatchRule(store);
        var item = CreateWorkItem("Weekly Report", "This is a critical update about the project");
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.True(result.Matched);
    }

    [Fact]
    public async Task EvaluateAsync_NoKeywordMatch_ReturnsNotMatched()
    {
        var store = Substitute.For<IAlertRuleStore>();
        store.GetKeywordsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string> { "urgent", "critical" });
        var rule = new KeywordMatchRule(store);
        var item = CreateWorkItem("Weekly Report", "Everything is fine");
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.False(result.Matched);
    }

    [Fact]
    public async Task EvaluateAsync_MultipleKeywords_AnyMatchTriggers()
    {
        var store = Substitute.For<IAlertRuleStore>();
        store.GetKeywordsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string> { "urgent", "critical", "security" });
        var rule = new KeywordMatchRule(store);
        var item = CreateWorkItem("Security Update Required");
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.True(result.Matched);
    }

    [Fact]
    public void Priority_ReturnsThirty()
    {
        var store = Substitute.For<IAlertRuleStore>();
        var rule = new KeywordMatchRule(store);

        Assert.Equal(30, rule.Priority);
    }

    [Fact]
    public async Task EvaluateAsync_UsesActionNeededSignal_WhenTypedContextIsAvailable()
    {
        var store = Substitute.For<IAlertRuleStore>();
        var rule = new KeywordMatchRule(store);
        var context = new EvaluationContext(
            CreateWorkItem("Status update"),
            userId: "user-1",
            focusState: null,
            normalizedSignals: new Dictionary<string, NormalizedSignal>
            {
                [WorkItemSignalKeys.ActionNeededSignal] = new BooleanSignal(WorkItemSignalKeys.ActionNeededSignal, true, "action needed")
            },
            priorityScore: null,
            approvedPolicy: UserTriagePolicy.Empty);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.True(result.Matched);
        Assert.Contains("action", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
