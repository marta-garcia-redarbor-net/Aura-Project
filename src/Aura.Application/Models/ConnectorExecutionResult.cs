namespace Aura.Application.Models;

public sealed record ConnectorExecutionResult(
    CheckpointIdentity Identity,
    int ItemCount,
    ConnectorExecutionStatus Status,
    string? FailureReason = null,
    DateTimeOffset? MaxProcessedAt = null);

public enum ConnectorExecutionStatus
{
    Success,
    Failure,
    PartialFailure
}
