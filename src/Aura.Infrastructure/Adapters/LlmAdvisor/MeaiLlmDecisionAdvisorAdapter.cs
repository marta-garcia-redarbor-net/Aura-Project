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
internal sealed partial class MeaiLlmDecisionAdvisorAdapter : ILlmDecisionAdvisor
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

            var parsed = TryParseJsonResponse(response.Text);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Rationale))
            {
                Log.ParseFailed(_logger, TruncateForLog(response.Text));
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

    /// <summary>
    /// Attempts to parse the LLM response as JSON using multiple strategies:
    /// 1. Direct parse of the full text
    /// 2. Extract JSON from ```json ... ``` markdown blocks
    /// 3. Find the outermost { ... } object in the text
    /// </summary>
    private static AdvisorJsonResponse? TryParseJsonResponse(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Strategy 1: direct parse
        var result = TryDeserialize(text);
        if (result is not null) return result;

        // Strategy 2: extract from markdown code block
        var markdownMatch = System.Text.RegularExpressions.Regex.Match(text, @"```(?:json)?\s*(\{.*?\})\s*```", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (markdownMatch.Success)
        {
            result = TryDeserialize(markdownMatch.Groups[1].Value);
            if (result is not null) return result;
        }

        // Strategy 3: find outermost { ... } object
        var firstBrace = text.IndexOf('{');
        var lastBrace = text.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
        {
            var candidate = text.Substring(firstBrace, lastBrace - firstBrace + 1);
            result = TryDeserialize(candidate);
            if (result is not null) return result;
        }

        return null;
    }

    private static AdvisorJsonResponse? TryDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<AdvisorJsonResponse>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string TruncateForLog(string text, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Length <= maxLength ? text : text[..maxLength] + "...";
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

    private static partial class Log
    {
        [LoggerMessage(EventId = 8001, Level = LogLevel.Warning,
            Message = "Failed to parse LLM response as advisory JSON. Raw: {RawText}")]
        public static partial void ParseFailed(ILogger logger, string? rawText);
    }

    private sealed record AdvisorJsonResponse(string? SuggestedVerdict, string Rationale, double? Confidence);
}
