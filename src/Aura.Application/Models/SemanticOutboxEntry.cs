using Aura.Domain.SemanticIndex.Enums;

namespace Aura.Application.Models;

/// <summary>
/// Represents a pending item in the semantic index outbox.
/// Canonical data is enqueued here; the sync worker processes entries asynchronously.
/// </summary>
public sealed class SemanticOutboxEntry
{
    public Guid Id { get; }
    public string CanonicalSourceId { get; }
    public string Content { get; }
    public SemanticCollectionType Collection { get; }
    public DateTimeOffset CreatedAt { get; }
    public bool Processed { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? Error { get; private set; }

    public SemanticOutboxEntry(
        Guid id,
        string canonicalSourceId,
        string content,
        SemanticCollectionType collection,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id must not be empty.", nameof(id));
        if (string.IsNullOrEmpty(canonicalSourceId))
            throw new ArgumentException("CanonicalSourceId must not be null or empty.", nameof(canonicalSourceId));
        if (string.IsNullOrEmpty(content))
            throw new ArgumentException("Content must not be null or empty.", nameof(content));

        Id = id;
        CanonicalSourceId = canonicalSourceId;
        Content = content;
        Collection = collection;
        CreatedAt = createdAt;
    }

    /// <summary>Marks this entry as successfully processed.</summary>
    public void MarkProcessed()
    {
        Processed = true;
        ProcessedAt = DateTimeOffset.UtcNow;
        Error = null;
    }

    /// <summary>Records a processing failure without marking as processed.</summary>
    public void MarkFailed(string error)
    {
        Error = error;
    }
}
