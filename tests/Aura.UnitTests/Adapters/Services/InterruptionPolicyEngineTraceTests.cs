using Aura.Application.Models;
using Aura.Application.Ports;
using FocusStateModel = Aura.Domain.FocusState.FocusState;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Services;

namespace Aura.UnitTests.Adapters.Services;

public class InterruptionPolicyEngineTraceTests
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

    private sealed class StubFocusStateResolver(FocusStateModel focusState) : IFocusStateResolver
    {
        public Task<FocusStateModel> ResolveAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(focusState);
    }

    private sealed class StubPolicyProvider(UserTriagePolicy policy) : IUserTriagePolicyProvider
    {
        public Task<UserTriagePolicy> GetApprovedPolicyAsync(string userId, CancellationToken ct)
            => Task.FromResult(policy);
    }

    private sealed class StubPriorityScoringService(PriorityScore score) : IPriorityScoringService
    {
        public PriorityScore Score(EvaluationContext context) => score;
    }

    private sealed class StubRule(RuleResult result) : IInterruptionRule
    {
        public int Priority => 10;

        public Task<RuleResult> EvaluateAsync(EvaluationContext context, CancellationToken ct)
            => Task.FromResult(result);
    }

    private sealed class StubDecisionContextRetriever(IReadOnlyList<DecisionContextItem> context) : IDecisionContextRetriever
    {
        public Task<IReadOnlyList<DecisionContextItem>> RetrieveAsync(WorkItem item, CancellationToken ct)
            => Task.FromResult(context);
    }

    private sealed class StubLlmDecisionAdvisor(AdvisoryResponse response) : ILlmDecisionAdvisor
    {
        public Task<AdvisoryResponse> EvaluateAsync(AdvisoryRequest request, CancellationToken ct)
            => Task.FromResult(response);
    }

    private static WorkItem CreateWorkItem(WorkItemPriority priority = WorkItemPriority.High)
        => new(
            externalId: "trace-001",
            title: "Urgent reviewer ping",
            source: "messages",
            sourceType: WorkItemSourceType.TeamsMessage,
            priority: priority,
            metadata: new Dictionary<string, string>
            {
                ["assignedTo"] = "user-1",
                [WorkItemSignalKeys.CanonicalSender] = "vip@aura.dev",
                [WorkItemSignalKeys.ActionNeededSignal] = "true"
            });

    private static FocusStateModel CreateWindowState() => new();

    [Fact]
    public async Task EvaluateAsync_WhenAdvisorConfirms_PersistsConfirmedGuardrailOutcome()
    {
        var store = new StubInterruptionDecisionStore();
        var engine = new InterruptionPolicyEngine(
            rules: [new StubRule(new RuleResult("ScoreThresholdRule", false, 0, 0.9, "no match"))],
            focusStateResolver: new StubFocusStateResolver(CreateWindowState()),
            policyProvider: new StubPolicyProvider(UserTriagePolicy.Empty),
            priorityScoringService: new StubPriorityScoringService(new PriorityScore("default-queue", 10, false, false, "queued", [])),
            decisionStore: store,
            decisionContextRetriever: new StubDecisionContextRetriever([]),
            llmDecisionAdvisor: new StubLlmDecisionAdvisor(new AdvisoryResponse(
                SuggestedVerdict: "QUEUE",
                Rationale: "Deterministic queue is appropriate.",
                GuardrailOutcome: "confirmed",
                Confidence: 0.92)));

        _ = await engine.EvaluateAsync(CreateWorkItem(), CancellationToken.None);

        Assert.Single(store.Records);
        Assert.Equal("confirmed", store.Records[0].GuardrailOutcome);
    }

    [Fact]
    public async Task EvaluateAsync_WhenAdvisorAdjustsWithinGuardrails_PersistsAdjustedGuardrailOutcome()
    {
        var store = new StubInterruptionDecisionStore();
        var engine = new InterruptionPolicyEngine(
            rules: [],
            focusStateResolver: new StubFocusStateResolver(CreateWindowState()),
            policyProvider: new StubPolicyProvider(UserTriagePolicy.Empty),
            priorityScoringService: new StubPriorityScoringService(new PriorityScore("default-queue", 10, false, false, "queued", [])),
            decisionStore: store,
            decisionContextRetriever: new StubDecisionContextRetriever([]),
            llmDecisionAdvisor: new StubLlmDecisionAdvisor(new AdvisoryResponse(
                SuggestedVerdict: "INTERRUPT",
                Rationale: "VIP + action-needed should interrupt now.",
                GuardrailOutcome: "adjusted",
                Confidence: 0.91)));

        _ = await engine.EvaluateAsync(CreateWorkItem(), CancellationToken.None);

        Assert.Single(store.Records);
        Assert.Equal("adjusted", store.Records[0].GuardrailOutcome);
        Assert.Equal("INTERRUPT", store.Records[0].Decision);
        Assert.Contains("VIP", store.Records[0].LlmRationale ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EvaluateAsync_WhenAdvisorSuggestsChangeForCriticalInterrupt_BlocksAdjustment()
    {
        var store = new StubInterruptionDecisionStore();
        var engine = new InterruptionPolicyEngine(
            rules: [],
            focusStateResolver: new StubFocusStateResolver(CreateWindowState()),
            policyProvider: new StubPolicyProvider(UserTriagePolicy.Empty),
            priorityScoringService: new StubPriorityScoringService(new PriorityScore("vip-action-needed", 120, true, true, "critical", [])),
            decisionStore: store,
            decisionContextRetriever: new StubDecisionContextRetriever([]),
            llmDecisionAdvisor: new StubLlmDecisionAdvisor(new AdvisoryResponse(
                SuggestedVerdict: "DEFER",
                Rationale: "Suggest defer due to probable interruption fatigue.",
                GuardrailOutcome: "adjusted",
                Confidence: 0.89)));

        _ = await engine.EvaluateAsync(CreateWorkItem(WorkItemPriority.Critical), CancellationToken.None);

        Assert.Single(store.Records);
        Assert.Equal("blocked", store.Records[0].GuardrailOutcome);
        Assert.Equal("INTERRUPT", store.Records[0].Decision);
    }

    [Fact]
    public async Task EvaluateAsync_WhenAdvisorUnavailable_PersistsLlmUnavailableAndKeepsDeterministic()
    {
        var store = new StubInterruptionDecisionStore();
        var engine = new InterruptionPolicyEngine(
            rules: [],
            focusStateResolver: new StubFocusStateResolver(CreateWindowState()),
            policyProvider: new StubPolicyProvider(UserTriagePolicy.Empty),
            priorityScoringService: new StubPriorityScoringService(new PriorityScore("default-queue", 10, false, false, "queued", [])),
            decisionStore: store,
            decisionContextRetriever: new StubDecisionContextRetriever([]),
            llmDecisionAdvisor: new StubLlmDecisionAdvisor(new AdvisoryResponse(
                SuggestedVerdict: null,
                Rationale: "Advisor unavailable.",
                GuardrailOutcome: "llm-unavailable",
                FailureReason: "timeout",
                Confidence: null)));

        _ = await engine.EvaluateAsync(CreateWorkItem(), CancellationToken.None);

        Assert.Single(store.Records);
        Assert.Equal("llm-unavailable", store.Records[0].GuardrailOutcome);
        Assert.Equal("QUEUE", store.Records[0].Decision);
        Assert.NotNull(store.Records[0].LlmRationale);
    }
}
