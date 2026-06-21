using Aura.Domain.WorkItems;

namespace Aura.Application.Ports;

public interface IWorkItemBuffer
{
    void Enqueue(WorkItem item);

    IReadOnlyList<WorkItem> Drain();
}
