using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Services.Rules;
using NSubstitute;

namespace Aura.UnitTests.Services.Rules;

public class VipSenderRuleTests
{
    private static WorkItem CreateWorkItem(string fromEmail)
    {
        var metadata = new Dictionary<string, string>
        {
            ["outlook.from"] = fromEmail
        };
        return new WorkItem("ext-1", "Test Subject", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium, metadata);
    }

    [Fact]
    public async Task EvaluateAsync_SenderInVipList_ReturnsMatched()
    {
        var store = Substitute.For<IAlertRuleStore>();
        store.GetVipSendersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string> { "boss@company.com", "vip@company.com" });
        var rule = new VipSenderRule(store);
        var item = CreateWorkItem("boss@company.com");
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.True(result.Matched);
        Assert.Equal("VipSenderRule", result.RuleName);
    }

    [Fact]
    public async Task EvaluateAsync_SenderNotInVipList_ReturnsNotMatched()
    {
        var store = Substitute.For<IAlertRuleStore>();
        store.GetVipSendersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string> { "boss@company.com" });
        var rule = new VipSenderRule(store);
        var item = CreateWorkItem("regular@company.com");
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.False(result.Matched);
    }

    [Fact]
    public async Task EvaluateAsync_EmptyVipList_ReturnsNotMatched()
    {
        var store = Substitute.For<IAlertRuleStore>();
        store.GetVipSendersAsync(Arg.Any<CancellationToken>())
            .Returns(new List<string>());
        var rule = new VipSenderRule(store);
        var item = CreateWorkItem("boss@company.com");
        var context = new EvaluationContext(item);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.False(result.Matched);
    }

    [Fact]
    public void Priority_ReturnsTwenty()
    {
        var store = Substitute.For<IAlertRuleStore>();
        var rule = new VipSenderRule(store);

        Assert.Equal(20, rule.Priority);
    }

    [Fact]
    public async Task EvaluateAsync_UsesNormalizedVipSenderSignal_WhenContextProvidesTypedSignals()
    {
        var store = Substitute.For<IAlertRuleStore>();
        var rule = new VipSenderRule(store);
        var context = new EvaluationContext(
            CreateWorkItem("regular@company.com"),
            userId: "user-1",
            focusState: null,
            normalizedSignals: new Dictionary<string, NormalizedSignal>
            {
                [WorkItemSignalKeys.VipSenderSignal] = new BooleanSignal(WorkItemSignalKeys.VipSenderSignal, true, "sender is vip")
            },
            priorityScore: null,
            approvedPolicy: UserTriagePolicy.Empty);

        var result = await rule.EvaluateAsync(context, CancellationToken.None);

        Assert.True(result.Matched);
        Assert.Contains("vip", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
