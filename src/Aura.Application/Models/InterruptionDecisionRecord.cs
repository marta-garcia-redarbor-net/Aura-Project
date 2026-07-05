namespace Aura.Application.Models;

/// <summary>
/// Represents a single interruption decision recorded by the policy engine.
/// All verdicts (INTERRUPT, QUEUE, DEFER) are persisted for audit.
/// </summary>
public sealed record InterruptionDecisionRecord(
    Guid WorkItemId,
    string Title,
    string SourceType,
    string Decision,
    int? PriorityScore,
    string Explanation,
    DateTimeOffset Timestamp,
    string FocusState);
