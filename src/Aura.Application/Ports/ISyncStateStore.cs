using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Port for persisting and reading per-source sync state (last timestamp, count, status).
/// </summary>
public interface ISyncStateStore
{
    Task<IReadOnlyList<SourceSyncState>> GetAllAsync(CancellationToken ct);
    Task UpdateAsync(string source, SourceSyncState state, CancellationToken ct);
}
