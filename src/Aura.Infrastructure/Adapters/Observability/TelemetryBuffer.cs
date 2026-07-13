using System.Collections.Concurrent;

namespace Aura.Infrastructure.Observability;

/// <summary>
/// Thread-safe bounded ring buffer. Producers never block — oldest entry evicted when full.
/// </summary>
public class TelemetryBuffer<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly int _capacity;
    private readonly object _trimLock = new();

    public TelemetryBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public void Write(T item)
    {
        _queue.Enqueue(item);
        if (_queue.Count > _capacity)
        {
            lock (_trimLock)
            {
                while (_queue.Count > _capacity && _queue.TryDequeue(out _)) { }
            }
        }
    }

    public IReadOnlyList<T> Snapshot()
    {
        return _queue.ToArray();
    }

    public int Count => _queue.Count;
}
