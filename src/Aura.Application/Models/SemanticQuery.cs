using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;

namespace Aura.Application.Models;

/// <summary>
/// Query DTO for semantic context retrieval.
/// </summary>
public sealed record SemanticQuery
{
    /// <summary>Natural language query text.</summary>
    public required string Text { get; init; }

    /// <summary>Optional collection filter. When null, searches all collections.</summary>
    public SemanticCollectionType? Collection { get; init; }

    /// <summary>Optional domain tag filters for narrowing results.</summary>
    public IReadOnlyList<DomainTag> TagFilters { get; init; } = [];

    /// <summary>Maximum number of results to return.</summary>
    public int TopK { get; init; } = 10;
}
