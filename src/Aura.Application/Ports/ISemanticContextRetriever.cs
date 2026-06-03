using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Port for retrieving scored semantic context. Used by Reviewer and Triage use cases.
/// Implementation lives in Infrastructure — never reference SDK types here.
/// </summary>
public interface ISemanticContextRetriever
{
    /// <summary>
    /// Retrieves semantically relevant chunks matching the query.
    /// Implementations MUST validate canonical source existence and discard orphans.
    /// </summary>
    Task<IReadOnlyList<ScoredSemanticChunk>> RetrieveAsync(SemanticQuery query, CancellationToken ct);
}
