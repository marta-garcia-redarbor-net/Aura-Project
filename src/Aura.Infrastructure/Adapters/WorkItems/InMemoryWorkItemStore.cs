using System.Collections.Concurrent;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;

namespace Aura.Infrastructure.Adapters.WorkItems;

internal sealed class InMemoryWorkItemStore : IWorkItemStore
{
    private readonly ConcurrentDictionary<string, WorkItem> _store = new(StringComparer.Ordinal);

    public Task<WorkItemPersistenceResult> SaveAsync(WorkItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ct.ThrowIfCancellationRequested();

        if (item.ExternalId.Contains("fail", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(WorkItemPersistenceResult.Failure("Simulated persistence failure for testing."));
        }

        // Dedup with stable Priority: first write wins for Priority.
        _store.AddOrUpdate(
            item.ExternalId,
            _ => item,
            (_, existing) =>
            {
                // Stable Priority — retain original; update everything else.
                return new WorkItem(
                    existing.ExternalId,
                    item.Title,
                    item.Source,
                    item.SourceType,
                    existing.Priority,
                    item.Metadata,
                    item.CorrelationId,
                    item.CapturedAtUtc);
            });

        return Task.FromResult(WorkItemPersistenceResult.Success());
    }

    public Task<WorkItem?> FindByExternalIdAsync(string externalId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(externalId);
        ct.ThrowIfCancellationRequested();

        _store.TryGetValue(externalId, out var item);
        return Task.FromResult(item);
    }
}
