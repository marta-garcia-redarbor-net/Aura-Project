using System.Collections.Concurrent;
using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// In-memory implementation of <see cref="ISyncStateStore"/>.
/// Suitable for single-instance deployments; future upgrade to Redis/SQLite per roadmap.
/// </summary>
internal sealed class InMemorySyncStateStore : ISyncStateStore
{
    private readonly ConcurrentDictionary<string, SourceSyncState> _states = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyList<SourceSyncState>> GetAllAsync(CancellationToken ct)
    {
        IReadOnlyList<SourceSyncState> result = _states.Values.ToArray();
        return Task.FromResult(result);
    }

    public Task UpdateAsync(string source, SourceSyncState state, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(state);

        _states[source] = state;
        return Task.CompletedTask;
    }
}
