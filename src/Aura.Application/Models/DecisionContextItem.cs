namespace Aura.Application.Models;

/// <summary>
/// Decision-time semantic context item consumed by interruption decisioning.
/// Keeps vector-store details out of Application/Domain layers.
/// </summary>
public sealed record DecisionContextItem(
    string CanonicalSourceId,
    string ContentSnippet,
    string SourceType,
    double RelevanceScore);
