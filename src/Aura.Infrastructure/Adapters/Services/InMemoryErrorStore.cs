using System.Collections.Concurrent;
using Aura.Application.Ports;

namespace Aura.Infrastructure.Adapters.Services;

/// <summary>
/// Thread-safe in-memory ring buffer for error entries used by the dashboard.
/// Default capacity is 100 entries; oldest entries are evicted when the buffer is full.
/// Registered as a singleton, so all hosts share the same in-process store.
/// </summary>
public sealed class InMemoryErrorStore : IErrorStore
{
    private readonly ConcurrentQueue<ErrorEntry> _entries = new();
    private readonly int _capacity;
    private readonly object _trimLock = new();

    /// <summary>
    /// Initializes a new instance with the specified capacity.
    /// </summary>
    public InMemoryErrorStore(int capacity = 100)
    {
        _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
    }

    /// <inheritdoc />
    public Task RecordAsync(ErrorEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ct.ThrowIfCancellationRequested();

        _entries.Enqueue(entry);

        // Trim oldest entries when over capacity
        if (_entries.Count > _capacity)
        {
            lock (_trimLock)
            {
                while (_entries.Count > _capacity && _entries.TryDequeue(out _)) { }
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ErrorEntry>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var all = _entries.ToArray();
        var take = Math.Min(count, all.Length);
        var result = new ErrorEntry[take];

        // Return most recent first (reverse order)
        Array.Copy(all, all.Length - take, result, 0, take);
        Array.Reverse(result);

        return Task.FromResult<IReadOnlyList<ErrorEntry>>(result);
    }
}
