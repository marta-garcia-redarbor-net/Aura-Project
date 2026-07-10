using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;

namespace Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;

internal sealed class NullDecisionContextRetriever : IDecisionContextRetriever
{
    public Task<IReadOnlyList<DecisionContextItem>> RetrieveAsync(WorkItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ct.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<DecisionContextItem>>([]);
    }
}
