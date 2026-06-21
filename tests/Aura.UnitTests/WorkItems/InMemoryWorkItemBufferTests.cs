using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.WorkItems;

namespace Aura.UnitTests.WorkItems;

public class InMemoryWorkItemBufferTests
{
    [Fact]
    public void EnqueueThenDrain_ReturnsAllItems()
    {
        var buffer = new InMemoryWorkItemBuffer();
        var first = CreateWorkItem("msg-1");
        var second = CreateWorkItem("msg-2");

        buffer.Enqueue(first);
        buffer.Enqueue(second);

        var drained = buffer.Drain();

        Assert.Equal(2, drained.Count);
        Assert.Contains(first, drained);
        Assert.Contains(second, drained);
    }

    [Fact]
    public void Drain_EmptiesBuffer()
    {
        var buffer = new InMemoryWorkItemBuffer();
        buffer.Enqueue(CreateWorkItem("msg-1"));

        _ = buffer.Drain();
        var drainedAgain = buffer.Drain();

        Assert.Empty(drainedAgain);
    }

    private static WorkItem CreateWorkItem(string externalId) =>
        new(
            externalId,
            $"title-{externalId}",
            "messages",
            WorkItemSourceType.TeamsMessage,
            WorkItemPriority.Medium,
            new Dictionary<string, string>());
}
