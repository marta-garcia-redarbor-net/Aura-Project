using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.Services;
using FocusStateDomain = Aura.Domain.FocusState.FocusState;
using FocusStateType = Aura.Domain.FocusState.FocusStateType;
using Aura.Domain.WorkItems;
using NSubstitute;

namespace Aura.UnitTests.Triage;

public sealed class PriorityScoringServiceTests
{
    [Fact]
    public void Score_SameUserAndSameCanonicalInputs_ReturnsSameRuleAndExplanation()
    {
        var store = Substitute.For<IAlertRuleStore>();
        store.GetVipSendersAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>(["boss@aura.dev"]));
        store.GetKeywordsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>(["approve", "review"]));

        var service = new PriorityScoringService(store);
        var context = new EvaluationContext(
            item: CreateWorkItem(new Dictionary<string, string>
            {
                [WorkItemSignalKeys.CanonicalSender] = "boss@aura.dev",
                [WorkItemSignalKeys.CanonicalSnippet] = "Please review the urgent incident response",
                [WorkItemSignalKeys.ActionNeededSignal] = "true",
                [WorkItemSignalKeys.TimeCriticalitySignal] = "high",
                [WorkItemSignalKeys.MessageLengthBucketSignal] = "short"
            }),
            userId: "user-1",
            focusState: CreateFocusState(FocusStateType.WindowOfOpportunity),
            approvedPolicy: UserTriagePolicy.Empty);

        var first = service.Score(context);
        var second = service.Score(context);

        Assert.Equal(first.RuleKey, second.RuleKey);
        Assert.Equal(first.Explanation, second.Explanation);
        Assert.Equal(first.InterruptionRank, second.InterruptionRank);
    }

    [Fact]
    public void Score_ContentCueFactorsRemainTraceable()
    {
        var store = Substitute.For<IAlertRuleStore>();
        store.GetVipSendersAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>([]));
        store.GetKeywordsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>(["review"]));

        var service = new PriorityScoringService(store);
        var context = new EvaluationContext(
            item: CreateWorkItem(new Dictionary<string, string>
            {
                [WorkItemSignalKeys.CanonicalSender] = "ops@aura.dev",
                [WorkItemSignalKeys.CanonicalSnippet] = "Need your review before noon",
                [WorkItemSignalKeys.ActionNeededSignal] = "true",
                [WorkItemSignalKeys.TimeCriticalitySignal] = "critical"
            }),
            userId: "user-1",
            focusState: CreateFocusState(FocusStateType.WindowOfOpportunity),
            approvedPolicy: UserTriagePolicy.Empty);

        var score = service.Score(context);

        Assert.Contains(score.Factors, factor => factor.Key == WorkItemSignalKeys.ActionNeededSignal);
        Assert.Contains(score.Factors, factor => factor.Key == WorkItemSignalKeys.TimeCriticalitySignal);
        Assert.DoesNotContain("opaque", score.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Score_ExplicitPerUserVipPolicyChangesResult()
    {
        var store = Substitute.For<IAlertRuleStore>();
        store.GetVipSendersAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>([]));
        store.GetKeywordsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<string>>(["review"]));

        var service = new PriorityScoringService(store);
        var item = CreateWorkItem(new Dictionary<string, string>
        {
            [WorkItemSignalKeys.CanonicalSender] = "boss@aura.dev",
            [WorkItemSignalKeys.CanonicalSnippet] = "Please review this today",
            [WorkItemSignalKeys.ActionNeededSignal] = "true"
        });

        var baselineContext = new EvaluationContext(
            item: item,
            userId: "user-a",
            focusState: CreateFocusState(FocusStateType.WindowOfOpportunity),
            approvedPolicy: UserTriagePolicy.Empty);

        var personalizedContext = new EvaluationContext(
            item: item,
            userId: "user-b",
            focusState: CreateFocusState(FocusStateType.WindowOfOpportunity),
            approvedPolicy: new UserTriagePolicy
            {
                VipSenders = ["boss@aura.dev"]
            });

        var baseline = service.Score(baselineContext);
        var personalized = service.Score(personalizedContext);

        Assert.NotEqual(baseline.RuleKey, personalized.RuleKey);
        Assert.Contains("boss@aura.dev", personalized.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    private static WorkItem CreateWorkItem(IReadOnlyDictionary<string, string> metadata)
        => new(
            externalId: "ext-1",
            title: "Urgent review required",
            source: "inbox",
            sourceType: WorkItemSourceType.OutlookEmail,
            priority: WorkItemPriority.High,
            metadata: metadata);

    private static FocusStateDomain CreateFocusState(FocusStateType type)
    {
        var state = new FocusStateDomain();
        return type switch
        {
            FocusStateType.WindowOfOpportunity => state,
            FocusStateType.Away => TransitionToAway(state),
            FocusStateType.Recovery => TransitionToRecovery(state),
            FocusStateType.DeepWork => TransitionToDeepWork(state),
            _ => state
        };
    }

    private static FocusStateDomain TransitionToAway(FocusStateDomain state)
    {
        state.GoToAway();
        return state;
    }

    private static FocusStateDomain TransitionToRecovery(FocusStateDomain state)
    {
        state.GoToAway();
        state.GoToRecovery();
        return state;
    }

    private static FocusStateDomain TransitionToDeepWork(FocusStateDomain state)
    {
        state.GoToAway();
        state.TryEnterDeepWork();
        return state;
    }
}
