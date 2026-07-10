using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.FocusState;
using FocusState = Aura.Domain.FocusState.FocusState;
using FocusStateType = Aura.Domain.FocusState.FocusStateType;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Services;
using NSubstitute;

namespace Aura.UnitTests.Services;

public class InterruptionPolicyEngineTests
{
    private sealed class StubInterruptionDecisionStore : IInterruptionDecisionStore
    {
        public List<InterruptionDecisionRecord> Records { get; } = [];

        public Task RecordAsync(InterruptionDecisionRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<PagedResult<InterruptionDecisionRecord>> QueryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<InterruptionDecisionRecord>());

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            Records.Clear();
            return Task.CompletedTask;
        }
    }

    private sealed class StubFocusStateResolver : IFocusStateResolver
    {
        private readonly Aura.Domain.FocusState.FocusState _state;

        public StubFocusStateResolver(Aura.Domain.FocusState.FocusState state)
        {
            _state = state;
        }

        public Task<Aura.Domain.FocusState.FocusState> ResolveAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(_state);
    }

    private sealed class StubPriorityScoringService(PriorityScore score) : IPriorityScoringService
    {
        public PriorityScore Score(EvaluationContext context) => score;
    }

    private sealed class StubUserTriagePolicyProvider(UserTriagePolicy policy) : IUserTriagePolicyProvider
    {
        public Task<UserTriagePolicy> GetApprovedPolicyAsync(string userId, CancellationToken ct)
            => Task.FromResult(policy);
    }

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
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.CanonicalSender] = "boss@aura.dev",
                ["assignedTo"] = "user-1"
            });

    private static Aura.Domain.FocusState.FocusState CreateFocusState(FocusStateType type)
    {
        var state = new Aura.Domain.FocusState.FocusState();
        return type switch
        {
            FocusStateType.WindowOfOpportunity => state,
            FocusStateType.Away => TransitionToAway(state),
            FocusStateType.Recovery => TransitionToRecovery(state),
            FocusStateType.DeepWork => TransitionToDeepWork(state),
            _ => state
        };
    }

    private static Aura.Domain.FocusState.FocusState TransitionToAway(Aura.Domain.FocusState.FocusState state)
    {
        state.GoToAway();
        return state;
    }

    private static Aura.Domain.FocusState.FocusState TransitionToRecovery(Aura.Domain.FocusState.FocusState state)
    {
        state.GoToAway();
        state.GoToRecovery();
        return state;
    }

    private static Aura.Domain.FocusState.FocusState TransitionToDeepWork(Aura.Domain.FocusState.FocusState state)
    {
        state.GoToAway();
        state.TryEnterDeepWork();
        return state;
    }

    [Fact]
    public async Task EvaluateAsync_FirstRuleInterruptNow_ShortCircuitsAndReturnsInterruptNow()
    {
        var rule1 = new StubRule("Rule1", 10, CreateResult("Rule1", true));
        var rule2 = new StubRule("Rule2", 20, CreateResult("Rule2", false));
        var engine = new InterruptionPolicyEngine(
            new[] { rule1, rule2 },
            new StubFocusStateResolver(CreateFocusState(FocusStateType.WindowOfOpportunity)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore("rule1", 100, true, true, "rule1", [])),
            new StubInterruptionDecisionStore());
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
        var engine = new InterruptionPolicyEngine(
            new[] { rule1, rule2 },
            new StubFocusStateResolver(CreateFocusState(FocusStateType.WindowOfOpportunity)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore("default-queue", 0, false, false, "default", [])),
            new StubInterruptionDecisionStore());
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
        var engine = new InterruptionPolicyEngine(
            new[] { rule1, rule2 },
            new StubFocusStateResolver(CreateFocusState(FocusStateType.WindowOfOpportunity)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore("rule1", 100, true, true, "rule1", [])),
            new StubInterruptionDecisionStore());
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
        var engine = new InterruptionPolicyEngine(
            new[] { rule1, rule2 },
            new StubFocusStateResolver(CreateFocusState(FocusStateType.WindowOfOpportunity)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore("rule1", 100, true, true, "rule1", [])),
            new StubInterruptionDecisionStore());
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
        var engine = new InterruptionPolicyEngine(
            Array.Empty<IInterruptionRule>(),
            new StubFocusStateResolver(CreateFocusState(FocusStateType.WindowOfOpportunity)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore("default-queue", 0, false, false, "default", [])),
            new StubInterruptionDecisionStore());
        var item = CreateWorkItem();

        var verdict = await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Equal(InterruptionDecision.Queue, verdict.Decision);
        Assert.Empty(verdict.Report.Results);
    }

    [Fact]
    public async Task EvaluateAsync_WindowOfOpportunityWithUrgentActionNeeded_ReturnsInterruptWithDecisiveSignals()
    {
        var item = new WorkItem(
            "ext-2",
            "Urgent incident",
            "inbox",
            WorkItemSourceType.OutlookEmail,
            WorkItemPriority.Critical,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.CanonicalSender] = "boss@aura.dev",
                [WorkItemSignalKeys.ActionNeededSignal] = "true",
                ["assignedTo"] = "user-1"
            });

        var engine = new InterruptionPolicyEngine(
            Array.Empty<IInterruptionRule>(),
            new StubFocusStateResolver(CreateFocusState(FocusStateType.WindowOfOpportunity)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore(
                "urgent-action-needed",
                100,
                true,
                true,
                "urgent + action_needed",
                [new PriorityFactorContribution(WorkItemSignalKeys.ActionNeededSignal, "action needed")])),
            new StubInterruptionDecisionStore());

        var verdict = await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Equal(InterruptionDecision.InterruptNow, verdict.Decision);
        Assert.Contains("action", verdict.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("user-1", verdict.TargetUserId);
    }

    [Fact]
    public async Task EvaluateAsync_AwayWithoutCriticalInterruptionRule_ReturnsDefer()
    {
        var item = CreateWorkItem();

        var engine = new InterruptionPolicyEngine(
            Array.Empty<IInterruptionRule>(),
            new StubFocusStateResolver(CreateFocusState(FocusStateType.Away)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore(
                "queue-only",
                10,
                false,
                false,
                "routine",
                [])),
            new StubInterruptionDecisionStore());

        var verdict = await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Equal(InterruptionDecision.Defer, verdict.Decision);
        Assert.Contains("Away", verdict.Explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_ExplicitOverridePattern_AutoAppliesForNextSimilarCase()
    {
        var item = new WorkItem(
            "ext-3",
            "Review PR",
            "pr",
            WorkItemSourceType.PrReview,
            WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.ExplicitPatternKey] = "pr-review:repo-a",
                [WorkItemSignalKeys.TargetResponsibleUserId] = "reviewer-1"
            });

        var policy = new UserTriagePolicy
        {
            ExplicitOverrides =
            [
                new ExplicitTriageOverride(
                    "pr-review:repo-a",
                    InterruptionDecision.InterruptNow,
                    "Explicit per-user override for repo-a review",
                    true)
            ]
        };

        var engine = new InterruptionPolicyEngine(
            Array.Empty<IInterruptionRule>(),
            new StubFocusStateResolver(CreateFocusState(FocusStateType.Recovery)),
            new StubUserTriagePolicyProvider(policy),
            new StubPriorityScoringService(new PriorityScore(
                "queue-only",
                0,
                false,
                false,
                "routine",
                [])),
            new StubInterruptionDecisionStore());

        var verdict = await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Equal(InterruptionDecision.InterruptNow, verdict.Decision);
        Assert.Contains("override", verdict.Explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("reviewer-1", verdict.TargetUserId);
    }

    [Fact]
    public async Task EvaluateAsync_OverrideDecision_PersistsRecord()
    {
        var item = new WorkItem(
            "ext-override", "Override Item", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.ExplicitPatternKey] = "always-interrupt",
                [WorkItemSignalKeys.TargetResponsibleUserId] = "user-1"
            });
        var policy = new UserTriagePolicy
        {
            ExplicitOverrides =
            [
                new ExplicitTriageOverride(
                    "always-interrupt",
                    InterruptionDecision.InterruptNow,
                    "Always interrupt",
                    true)
            ]
        };
        var store = new StubInterruptionDecisionStore();
        var engine = new InterruptionPolicyEngine(
            Array.Empty<IInterruptionRule>(),
            new StubFocusStateResolver(CreateFocusState(FocusStateType.DeepWork)),
            new StubUserTriagePolicyProvider(policy),
            new StubPriorityScoringService(new PriorityScore("default", 0, false, false, "default", [])),
            store);

        await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Single(store.Records);
        var record = store.Records[0];
        Assert.Equal(item.Id, record.WorkItemId);
        Assert.Equal("Override Item", record.Title);
        Assert.Equal("INTERRUPT", record.Decision);
        Assert.Null(record.PriorityScore);
    }

    [Fact]
    public async Task EvaluateAsync_QueueDecision_PersistsRecord()
    {
        var item = CreateWorkItem();
        var store = new StubInterruptionDecisionStore();
        var engine = new InterruptionPolicyEngine(
            Array.Empty<IInterruptionRule>(),
            new StubFocusStateResolver(CreateFocusState(FocusStateType.WindowOfOpportunity)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore("default-queue", 0, false, false, "default", [])),
            store);

        await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Single(store.Records);
        var record = store.Records[0];
        Assert.Equal(item.Id, record.WorkItemId);
        Assert.Equal("QUEUE", record.Decision);
    }

    [Fact]
    public async Task EvaluateAsync_DeferDecision_PersistsRecord()
    {
        var item = CreateWorkItem();
        var store = new StubInterruptionDecisionStore();
        var engine = new InterruptionPolicyEngine(
            Array.Empty<IInterruptionRule>(),
            new StubFocusStateResolver(CreateFocusState(FocusStateType.Away)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore("queue-only", 10, false, false, "routine", [])),
            store);

        await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Single(store.Records);
        var record = store.Records[0];
        Assert.Equal(item.Id, record.WorkItemId);
        Assert.Equal("DEFER", record.Decision);
    }

    [Fact]
    public async Task EvaluateAsync_InterruptDecision_PersistsRecord()
    {
        var item = CreateWorkItem();
        var store = new StubInterruptionDecisionStore();
        var interruptingRule = new StubRule("InterruptRule", 10, CreateResult("InterruptRule", true));

        var engine = new InterruptionPolicyEngine(
            new[] { interruptingRule },
            new StubFocusStateResolver(CreateFocusState(FocusStateType.WindowOfOpportunity)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore("interrupt", 90, true, true, "urgent", [])),
            store);

        await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Single(store.Records);
        var record = store.Records[0];
        Assert.Equal(item.Id, record.WorkItemId);
        Assert.Equal("INTERRUPT", record.Decision);
        Assert.Equal(90, record.PriorityScore);
    }

    [Fact]
    public async Task EvaluateAsync_PersistRecordContainsPriorityScore()
    {
        var rule1 = new StubRule("Rule1", 10, CreateResult("Rule1", true));
        var store = new StubInterruptionDecisionStore();
        var engine = new InterruptionPolicyEngine(
            new[] { rule1 },
            new StubFocusStateResolver(CreateFocusState(FocusStateType.WindowOfOpportunity)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore("rule1", 75, true, false, "matched", [])),
            store);
        var item = CreateWorkItem();

        await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Single(store.Records);
        var record = store.Records[0];
        Assert.NotNull(record.PriorityScore);
        Assert.Equal(75, record.PriorityScore);
    }

    [Fact]
    public async Task EvaluateAsync_PersistRecordContainsTimestamp()
    {
        var store = new StubInterruptionDecisionStore();
        var engine = new InterruptionPolicyEngine(
            Array.Empty<IInterruptionRule>(),
            new StubFocusStateResolver(CreateFocusState(FocusStateType.WindowOfOpportunity)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore("default", 0, false, false, "default", [])),
            store);
        var item = CreateWorkItem();

        var before = DateTimeOffset.UtcNow;
        await engine.EvaluateAsync(item, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        Assert.Single(store.Records);
        var record = store.Records[0];
        Assert.InRange(record.Timestamp, before, after);
    }

    [Fact]
    public async Task EvaluateAsync_PersistRecordContainsSourceType()
    {
        var item = CreateWorkItem();
        var store = new StubInterruptionDecisionStore();
        var engine = new InterruptionPolicyEngine(
            Array.Empty<IInterruptionRule>(),
            new StubFocusStateResolver(CreateFocusState(FocusStateType.WindowOfOpportunity)),
            new StubUserTriagePolicyProvider(UserTriagePolicy.Empty),
            new StubPriorityScoringService(new PriorityScore("default", 0, false, false, "default", [])),
            store);

        await engine.EvaluateAsync(item, CancellationToken.None);

        Assert.Single(store.Records);
        var record = store.Records[0];
        Assert.Equal("OutlookEmail", record.SourceType);
    }
}
