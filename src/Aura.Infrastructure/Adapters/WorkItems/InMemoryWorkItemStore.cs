using System.Collections.Concurrent;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;

namespace Aura.Infrastructure.Adapters.WorkItems;

internal sealed class InMemoryWorkItemStore : IWorkItemStore
{
    private readonly ConcurrentDictionary<Guid, WorkItem> _workItems = new();
    private readonly Lock _gate = new();

    public Task<WorkItemPersistenceResult> SaveAsync(WorkItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ct.ThrowIfCancellationRequested();

        if (item.ExternalId.Contains("fail", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(WorkItemPersistenceResult.Failure("Simulated persistence failure for testing."));
        }

        lock (_gate)
        {
            _workItems[item.Id] = item;
        }

        return Task.FromResult(WorkItemPersistenceResult.Success());
    }
}
