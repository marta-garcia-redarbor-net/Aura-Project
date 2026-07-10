using System.Text.Json;
using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.Infrastructure.Adapters.LlmAdvisor;

/// <summary>
/// LLM decision advisor adapter using Microsoft.Extensions.AI IChatClient.
/// </summary>
internal sealed class MeaiLlmDecisionAdvisorAdapter : ILlmDecisionAdvisor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatClient _chatClient;
    private readonly LlmAdvisorOptions _options;
    private readonly ILogger<MeaiLlmDecisionAdvisorAdapter> _logger;

    public MeaiLlmDecisionAdvisorAdapter(
        IChatClient chatClient,
        IOptions<LlmAdvisorOptions> options,
        ILogger<MeaiLlmDecisionAdvisorAdapter> logger)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AdvisoryResponse> EvaluateAsync(AdvisoryRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));

        try
        {
            var userPrompt = BuildPrompt(request);
            var response = await _chatClient.GetResponseAsync(
                [new ChatMessage(ChatRole.User, userPrompt)],
                options: new ChatOptions(),
                cancellationToken: timeoutCts.Token);

            if (string.IsNullOrWhiteSpace(response.Text))
            {
                return new AdvisoryResponse(
                    SuggestedVerdict: null,
                    Rationale: "LLM returned an empty response.",
                    GuardrailOutcome: "llm-unavailable",
                    FailureReason: "empty-response");
            }

            AdvisorJsonResponse? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<AdvisorJsonResponse>(response.Text, JsonOptions);
            }
            catch (JsonException)
            {
                return new AdvisoryResponse(
                    SuggestedVerdict: null,
                    Rationale: "LLM response could not be parsed as advisory JSON.",
                    GuardrailOutcome: "llm-unavailable",
                    FailureReason: "json-parse-failed");
            }

            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Rationale))
            {
                return new AdvisoryResponse(
                    SuggestedVerdict: null,
                    Rationale: "LLM response could not be parsed as advisory JSON.",
                    GuardrailOutcome: "llm-unavailable",
                    FailureReason: "json-parse-failed");
            }

            if (parsed.Confidence is double confidence && confidence < _options.ConfidenceThreshold)
            {
                return new AdvisoryResponse(
                    SuggestedVerdict: request.DeterministicVerdict,
                    Rationale: parsed.Rationale,
                    GuardrailOutcome: "confirmed",
                    FailureReason: $"confidence-below-threshold:{confidence:F2}",
                    Confidence: confidence);
            }

            var normalizedVerdict = NormalizeVerdict(parsed.SuggestedVerdict);
            var guardrailOutcome = string.Equals(normalizedVerdict, request.DeterministicVerdict, StringComparison.Ordinal)
                ? "confirmed"
                : "adjusted";

            return new AdvisoryResponse(
                SuggestedVerdict: normalizedVerdict,
                Rationale: parsed.Rationale,
                GuardrailOutcome: guardrailOutcome,
                FailureReason: null,
                Confidence: parsed.Confidence);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new AdvisoryResponse(
                SuggestedVerdict: null,
                Rationale: "LLM request timed out.",
                GuardrailOutcome: "llm-unavailable",
                FailureReason: "timeout");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM advisory call failed. Falling back to deterministic verdict.");
            return new AdvisoryResponse(
                SuggestedVerdict: null,
                Rationale: "LLM request failed.",
                GuardrailOutcome: "llm-unavailable",
                FailureReason: ex.GetType().Name);
        }
    }

    private static string BuildPrompt(AdvisoryRequest request)
    {
        var contextSummary = request.Context.Count == 0
            ? "[]"
            : string.Join("; ", request.Context.Select(c =>
                $"{c.SourceType}:{c.CanonicalSourceId} score={c.RelevanceScore:F2} snippet={c.ContentSnippet}"));

        return
            "You are an interruption-decision advisor.\n" +
            "Return strict JSON only in this shape:\n" +
            "{ \"suggestedVerdict\": \"INTERRUPT|QUEUE|DEFER|null\", \"rationale\": \"...\", \"confidence\": 0.0 }\n\n" +
            $"Work item title: {request.Item.Title}\n" +
            $"Deterministic verdict: {request.DeterministicVerdict}\n" +
            $"Signals: {string.Join(", ", request.Signals.Keys)}\n" +
            $"Retrieved context: {contextSummary}";
    }

    private static string? NormalizeVerdict(string? verdict)
    {
        if (string.IsNullOrWhiteSpace(verdict))
        {
            return null;
        }

        var normalized = verdict.Trim().ToUpperInvariant();
        return normalized is "INTERRUPT" or "QUEUE" or "DEFER" ? normalized : null;
    }

    private sealed record AdvisorJsonResponse(string? SuggestedVerdict, string Rationale, double? Confidence);
}
