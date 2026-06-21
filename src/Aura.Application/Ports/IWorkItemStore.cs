using Aura.Application.Models;
using Aura.Domain.WorkItems;

namespace Aura.Application.Ports;

public interface IWorkItemStore
{
    Task<WorkItemPersistenceResult> SaveAsync(WorkItem item, CancellationToken ct);
}
