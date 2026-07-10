namespace Aura.UI.Models;

public sealed record DecisionLogResponse(
    IReadOnlyList<DecisionLogItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}

public sealed record DecisionLogItemResponse(
    Guid WorkItemId,
    string Title,
    string SourceType,
    string Decision,
    int? PriorityScore,
    string Explanation,
    DateTimeOffset Timestamp,
    string FocusState,
    IReadOnlyList<DecisionContextItemResponse>? RetrievedSemanticContext = null,
    string? LlmRationale = null,
    string? GuardrailOutcome = null);

public sealed record DecisionContextItemResponse(
    string CanonicalSourceId,
    string ContentSnippet,
    string SourceType,
    double RelevanceScore);
