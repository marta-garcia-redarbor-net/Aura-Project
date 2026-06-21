using Aura.Application.Ports;
using Aura.Domain.WorkItems;

namespace Aura.Infrastructure.Adapters.WorkItems;

internal sealed class InMemoryWorkItemBuffer : IWorkItemBuffer
{
    private readonly Lock _gate = new();
    private List<WorkItem> _items = [];

    public void Enqueue(WorkItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (_gate)
        {
            _items.Add(item);
        }
    }

    public IReadOnlyList<WorkItem> Drain()
    {
        lock (_gate)
        {
            var drained = _items;
            _items = [];
            return drained;
        }
    }
}
