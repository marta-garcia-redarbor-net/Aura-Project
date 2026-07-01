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
}
