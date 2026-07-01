namespace Aura.Domain.WorkItems;

/// <summary>
/// Core domain entity representing a unit of work flowing through the kernel pipeline.
/// Encapsulates state transitions with invariant guards.
/// </summary>
public sealed class WorkItem
{
    private const string CurrentSchemaVersion = "v1";

    public Guid Id { get; }
    public string ExternalId { get; }
    public string Title { get; }
    public string Source { get; }
    public WorkItemSourceType SourceType { get; }
    public WorkItemPriority Priority { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }
    public string CorrelationId { get; }
    public DateTimeOffset CapturedAtUtc { get; }
    public string SchemaVersion { get; }
    public WorkItemStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public string? FaultReason { get; private set; }

    public WorkItem(
        string externalId,
        string title,
        string source,
        WorkItemSourceType sourceType,
        WorkItemPriority priority,
        IReadOnlyDictionary<string, string> metadata,
        string? correlationId = null,
        DateTimeOffset? capturedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(externalId))
            throw new ArgumentException("ExternalId must not be null or empty.", nameof(externalId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title must not be null or empty.", nameof(title));
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source must not be null or empty.", nameof(source));
        if (!Enum.IsDefined(sourceType))
            throw new ArgumentException("SourceType is outside the supported source set.", nameof(sourceType));
        if (!Enum.IsDefined(priority))
            throw new ArgumentException("Priority is outside the supported priority set.", nameof(priority));
        ArgumentNullException.ThrowIfNull(metadata);

        Id = Guid.NewGuid();
        ExternalId = externalId;
        Title = title;
        Source = source;
        SourceType = sourceType;
        Priority = priority;
        Metadata = metadata;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString()
            : correlationId;
        CapturedAtUtc = capturedAtUtc is null || capturedAtUtc == DateTimeOffset.MinValue
            ? DateTimeOffset.UtcNow
            : capturedAtUtc.Value;
        SchemaVersion = CurrentSchemaVersion;
        Status = WorkItemStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Transition from <see cref="WorkItemStatus.Pending"/> to <see cref="WorkItemStatus.Processing"/>.</summary>
    public void MarkProcessing()
    {
        if (Status != WorkItemStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot transition to Processing from {Status}. Expected Pending.");

        Status = WorkItemStatus.Processing;
    }

    /// <summary>Transition from <see cref="WorkItemStatus.Processing"/> to <see cref="WorkItemStatus.Completed"/>.</summary>
    public void MarkCompleted()
    {
        if (Status != WorkItemStatus.Processing)
            throw new InvalidOperationException(
                $"Cannot transition to Completed from {Status}. Expected Processing.");

        Status = WorkItemStatus.Completed;
    }

    /// <summary>Transition from <see cref="WorkItemStatus.Pending"/> to <see cref="WorkItemStatus.Completed"/>.
    /// Origin-side completion for auto-dismiss when a chat has been fully read.
    /// Throws <see cref="InvalidOperationException"/> when called from Processing, Completed, or Faulted.</summary>
    public void MarkAutoCompleted()
    {
        if (Status != WorkItemStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot auto-complete from {Status}. Expected Pending.");

        Status = WorkItemStatus.Completed;
    }

    /// <summary>Transition from <see cref="WorkItemStatus.Processing"/> to <see cref="WorkItemStatus.Faulted"/>.</summary>
    public void MarkFaulted(string reason)
    {
        if (string.IsNullOrEmpty(reason))
            throw new ArgumentException("Fault reason must not be null or empty.", nameof(reason));
        if (Status != WorkItemStatus.Processing)
            throw new InvalidOperationException(
                $"Cannot transition to Faulted from {Status}. Expected Processing.");

        Status = WorkItemStatus.Faulted;
        FaultReason = reason;
    }
}
