namespace Aura.Application.Models;

public sealed record ConnectorExecutionResult(
    CheckpointIdentity Identity,
    int ItemCount,
    ConnectorExecutionStatus Status,
    string? FailureReason = null);

public enum ConnectorExecutionStatus
{
    Success,
    Failure
}
