using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.LlmAdvisor;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Aura.UnitTests.Adapters.LlmAdvisor;

public class GuardrailTests
{
    private sealed class StubChatClient(Func<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken, Task<ChatResponse>> getResponse)
        : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => getResponse(messages, options, cancellationToken);

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private static AdvisoryRequest BuildRequest(string deterministic = "QUEUE")
        => new(
            Item: new WorkItem(
                externalId: "test-1",
                title: "Urgent incident",
                source: "messages",
                sourceType: WorkItemSourceType.TeamsMessage,
                priority: WorkItemPriority.High,
                metadata: new Dictionary<string, string>()),
            DeterministicVerdict: deterministic,
            Signals: new Dictionary<string, NormalizedSignal>(),
            Context: []);

    [Fact]
    public async Task EvaluateAsync_WhenValidJsonWithSameVerdict_ReturnsConfirmed()
    {
        var client = new StubChatClient((_, _, _) => Task.FromResult(new ChatResponse(
            new ChatMessage(ChatRole.Assistant, "{\"suggestedVerdict\":\"QUEUE\",\"rationale\":\"queue is appropriate\",\"confidence\":0.92}"))));

        var sut = new MeaiLlmDecisionAdvisorAdapter(
            client,
            Options.Create(new LlmAdvisorOptions { Enabled = true, ConfidenceThreshold = 0.7 }),
            NullLogger<MeaiLlmDecisionAdvisorAdapter>.Instance);

        var result = await sut.EvaluateAsync(BuildRequest(), CancellationToken.None);

        Assert.Equal("confirmed", result.GuardrailOutcome);
        Assert.Equal("QUEUE", result.SuggestedVerdict);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task EvaluateAsync_WhenLowConfidence_ReturnsConfirmedWithFailureReason()
    {
        var client = new StubChatClient((_, _, _) => Task.FromResult(new ChatResponse(
            new ChatMessage(ChatRole.Assistant, "{\"suggestedVerdict\":\"INTERRUPT\",\"rationale\":\"might be urgent\",\"confidence\":0.42}"))));

        var sut = new MeaiLlmDecisionAdvisorAdapter(
            client,
            Options.Create(new LlmAdvisorOptions { Enabled = true, ConfidenceThreshold = 0.7 }),
            NullLogger<MeaiLlmDecisionAdvisorAdapter>.Instance);

        var result = await sut.EvaluateAsync(BuildRequest(), CancellationToken.None);

        Assert.Equal("confirmed", result.GuardrailOutcome);
        Assert.Equal("QUEUE", result.SuggestedVerdict);
        Assert.StartsWith("confidence-below-threshold", result.FailureReason);
    }

    [Fact]
    public async Task EvaluateAsync_WhenJsonParseFails_ReturnsLlmUnavailable()
    {
        var client = new StubChatClient((_, _, _) => Task.FromResult(new ChatResponse(
            new ChatMessage(ChatRole.Assistant, "not-json"))));

        var sut = new MeaiLlmDecisionAdvisorAdapter(
            client,
            Options.Create(new LlmAdvisorOptions { Enabled = true }),
            NullLogger<MeaiLlmDecisionAdvisorAdapter>.Instance);

        var result = await sut.EvaluateAsync(BuildRequest(), CancellationToken.None);

        Assert.Equal("llm-unavailable", result.GuardrailOutcome);
        Assert.Equal("json-parse-failed", result.FailureReason);
    }

    [Fact]
    public async Task EvaluateAsync_WhenChatClientThrows_ReturnsLlmUnavailable()
    {
        var client = new StubChatClient((_, _, _) => throw new HttpRequestException("unavailable"));

        var sut = new MeaiLlmDecisionAdvisorAdapter(
            client,
            Options.Create(new LlmAdvisorOptions { Enabled = true }),
            NullLogger<MeaiLlmDecisionAdvisorAdapter>.Instance);

        var result = await sut.EvaluateAsync(BuildRequest(), CancellationToken.None);

        Assert.Equal("llm-unavailable", result.GuardrailOutcome);
        Assert.Equal("HttpRequestException", result.FailureReason);
    }
}
