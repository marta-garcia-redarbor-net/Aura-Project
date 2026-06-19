namespace Aura.Application.Models;

public sealed record WorkItemPersistenceResult
{
    public bool IsSuccess { get; init; }

    public string? FailureReason { get; init; }

    public static WorkItemPersistenceResult Success()
        => new() { IsSuccess = true };

    public static WorkItemPersistenceResult Failure(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Failure reason must not be null or empty.", nameof(reason));
        }

        return new WorkItemPersistenceResult { IsSuccess = false, FailureReason = reason };
    }
}
