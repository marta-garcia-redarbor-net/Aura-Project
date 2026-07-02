using Aura.Application.Models;
using Aura.Domain.WorkItems;

namespace Aura.Application.Ports;

public interface IWorkItemStore
{
    Task<WorkItemPersistenceResult> SaveAsync(WorkItem item, CancellationToken ct);

    /// <summary>
    /// Finds a work item by its external identifier, or null if not found.
    /// </summary>
    Task<WorkItem?> FindByExternalIdAsync(string externalId, CancellationToken ct);

    /// <summary>Retorna los ExternalIds de ítems Pending para un source type.</summary>
    Task<IReadOnlySet<string>> GetPendingExternalIdsAsync(
        WorkItemSourceType source, CancellationToken ct);

    /// <summary>Marca como Completed los ítems Pending con los ExternalIds dados.
    /// ExternalIds inexistentes se ignoran.</summary>
    Task MarkCompletedAsync(
        IReadOnlySet<string> externalIds, WorkItemSourceType source, CancellationToken ct);
}
