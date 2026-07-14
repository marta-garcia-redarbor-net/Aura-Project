using Aura.Infrastructure.Observability;

namespace Aura.UnitTests.Adapters.Observability;

public class TelemetryBufferTests
{
    [Fact]
    public void Write_WithinCapacity_AllItemsRetained()
    {
        var buffer = new TelemetryBuffer<string>(capacity: 5);

        buffer.Write("a");
        buffer.Write("b");
        buffer.Write("c");

        var snapshot = buffer.Snapshot();

        Assert.Equal(3, snapshot.Count);
        Assert.Equal("a", snapshot[0]);
        Assert.Equal("b", snapshot[1]);
        Assert.Equal("c", snapshot[2]);
    }

    [Fact]
    public void Write_ExceedsCapacity_EvictsOldest()
    {
        var buffer = new TelemetryBuffer<int>(capacity: 3);

        buffer.Write(1);
        buffer.Write(2);
        buffer.Write(3);
        buffer.Write(4); // evicts 1
        buffer.Write(5); // evicts 2

        var snapshot = buffer.Snapshot();

        Assert.Equal(3, snapshot.Count);
        Assert.Equal(3, snapshot[0]);
        Assert.Equal(4, snapshot[1]);
        Assert.Equal(5, snapshot[2]);
    }

    [Fact]
    public void Write_LargeOverflow_MaintainsCapacity()
    {
        var buffer = new TelemetryBuffer<int>(capacity: 5);

        for (int i = 0; i < 100; i++)
        {
            buffer.Write(i);
        }

        var snapshot = buffer.Snapshot();
        Assert.Equal(5, snapshot.Count);
        Assert.Equal(95, snapshot[0]);
        Assert.Equal(99, snapshot[4]);
    }

    [Fact]
    public void Snapshot_ReturnsCopy_ModifyingSnapshotDoesNotAffectBuffer()
    {
        var buffer = new TelemetryBuffer<string>(capacity: 5);
        buffer.Write("x");

        var snapshot = buffer.Snapshot();
        Assert.Single(snapshot);

        buffer.Write("y");
        Assert.Single(snapshot); // snapshot unchanged
        Assert.Equal(2, buffer.Count);
    }

    [Fact]
    public void Count_ReflectsCurrentItems()
    {
        var buffer = new TelemetryBuffer<string>(capacity: 3);

        Assert.Equal(0, buffer.Count);
        buffer.Write("a");
        Assert.Equal(1, buffer.Count);
        buffer.Write("b");
        Assert.Equal(2, buffer.Count);
        buffer.Write("c");
        Assert.Equal(3, buffer.Count);
        buffer.Write("d"); // evicts oldest
        Assert.Equal(3, buffer.Count);
    }

    [Fact]
    public void ConcurrentWrites_DoesNotLoseOrCorrupt()
    {
        var buffer = new TelemetryBuffer<int>(capacity: 1000);
        const int threadCount = 10;
        const int writesPerThread = 100;
        var tasks = new List<Task>();

        for (int t = 0; t < threadCount; t++)
        {
            var threadId = t;
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < writesPerThread; i++)
                {
                    buffer.Write(threadId * writesPerThread + i);
                }
            }));
        }

        Task.WaitAll(tasks);

        var snapshot = buffer.Snapshot();
        Assert.Equal(1000, snapshot.Count);
        Assert.Equal(1000, buffer.Count);
    }

    [Fact]
    public void Constructor_ZeroCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryBuffer<string>(0));
    }

    [Fact]
    public void Constructor_NegativeCapacity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryBuffer<string>(-1));
    }

    [Fact]
    public void Snapshot_EmptyBuffer_ReturnsEmptyList()
    {
        var buffer = new TelemetryBuffer<string>(5);

        var snapshot = buffer.Snapshot();

        Assert.Empty(snapshot);
    }
}
