using Aura.Application.Models;
using Aura.Domain.WorkItems;

namespace Aura.Application.Ports;

/// <summary>
/// Port for decision-time semantic retrieval used by interruption policy evaluation.
/// </summary>
public interface IDecisionContextRetriever
{
    Task<IReadOnlyList<DecisionContextItem>> RetrieveAsync(WorkItem item, CancellationToken ct);
}
