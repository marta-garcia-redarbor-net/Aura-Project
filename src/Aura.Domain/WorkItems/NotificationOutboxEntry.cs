namespace Aura.Domain.WorkItems;

/// <summary>
/// Entity representing a pending notification entry in the cross-process outbox.
/// Written by Workers.exe during ingestion and consumed by Api.exe via BackgroundService.
/// </summary>
public sealed class NotificationOutboxEntry
{
    public Guid Id { get; }
    public Guid WorkItemId { get; }
    public string UserId { get; }
    public string SourceType { get; }
    public string Title { get; }
    public double Priority { get; }
    public string? TriggerRule { get; }
    public string? Explanation { get; }
    public string? Decision { get; }
    public string? TargetUserId { get; }
    public string? RuleResults { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? DispatchedAt { get; private set; }

    /// <summary>
    /// Creates a new outbox entry for enqueueing. Auto-generates Id and CreatedAt.
    /// </summary>
    public NotificationOutboxEntry(
        Guid workItemId,
        string userId,
        string sourceType,
        string title,
        double priority,
        string? triggerRule = null,
        string? explanation = null,
        string? decision = null,
        string? targetUserId = null,
        string? ruleResults = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId must not be null or empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(sourceType))
            throw new ArgumentException("SourceType must not be null or empty.", nameof(sourceType));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title must not be null or empty.", nameof(title));

        Id = Guid.NewGuid();
        WorkItemId = workItemId;
        UserId = userId;
        SourceType = sourceType;
        Title = title;
        Priority = priority;
        TriggerRule = triggerRule;
        Explanation = explanation;
        Decision = decision;
        TargetUserId = targetUserId;
        RuleResults = ruleResults;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Creates an outbox entry from persisted data (used by store implementations).
    /// </summary>
    public NotificationOutboxEntry(
        Guid id,
        Guid workItemId,
        string userId,
        string sourceType,
        string title,
        double priority,
        string? triggerRule,
        DateTimeOffset createdAt,
        DateTimeOffset? dispatchedAt,
        string? explanation = null,
        string? decision = null,
        string? targetUserId = null,
        string? ruleResults = null)
    {
        Id = id;
        WorkItemId = workItemId;
        UserId = userId;
        SourceType = sourceType;
        Title = title;
        Priority = priority;
        TriggerRule = triggerRule;
        Explanation = explanation;
        Decision = decision;
        TargetUserId = targetUserId;
        RuleResults = ruleResults;
        CreatedAt = createdAt;
        DispatchedAt = dispatchedAt;
    }

    /// <summary>
    /// Marks this entry as dispatched by setting <see cref="DispatchedAt"/> to the current UTC time.
    /// </summary>
    public void MarkDispatched()
    {
        DispatchedAt = DateTimeOffset.UtcNow;
    }
}
