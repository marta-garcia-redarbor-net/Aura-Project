using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Infrastructure.Adapters.Ingestion;

internal sealed class InMemoryIngestionCheckpointStore : IIngestionCheckpointStore
{
    private readonly Dictionary<CheckpointIdentity, IngestionCheckpoint> _checkpoints = new();
    private readonly Lock _gate = new();

    public Task<IngestionCheckpoint?> GetAsync(CheckpointIdentity identity, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _checkpoints.TryGetValue(identity, out var checkpoint);
            return Task.FromResult<IngestionCheckpoint?>(checkpoint);
        }
    }

    public Task SaveAsync(CheckpointIdentity identity, IngestionCheckpoint checkpoint, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _checkpoints[identity] = checkpoint;
            return Task.CompletedTask;
        }
    }
}
