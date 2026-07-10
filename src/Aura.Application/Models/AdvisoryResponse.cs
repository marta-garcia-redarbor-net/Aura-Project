namespace Aura.Application.Models;

/// <summary>
/// Output contract for LLM decision advisory.
/// </summary>
public sealed record AdvisoryResponse(
    string? SuggestedVerdict,
    string Rationale,
    string GuardrailOutcome,
    string? FailureReason = null,
    double? Confidence = null);
