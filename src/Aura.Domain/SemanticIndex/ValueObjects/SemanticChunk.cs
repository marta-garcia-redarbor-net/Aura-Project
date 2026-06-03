using Aura.Domain.SemanticIndex.Enums;

namespace Aura.Domain.SemanticIndex.ValueObjects;

/// <summary>
/// Immutable derived semantic unit with metadata for contextual retrieval.
/// </summary>
public sealed record SemanticChunk
{
    public Guid Id { get; }
    public string CanonicalSourceId { get; }
    public string Content { get; }
    public SemanticCollectionType Collection { get; }
    public IReadOnlyList<DomainTag> Tags { get; }
    public DateTimeOffset CreatedAt { get; }

    public SemanticChunk(
        Guid id,
        string canonicalSourceId,
        string content,
        SemanticCollectionType collection,
        IReadOnlyList<DomainTag> tags,
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
        Tags = (tags ?? throw new ArgumentNullException(nameof(tags))).ToArray();
        CreatedAt = createdAt;
    }
}
