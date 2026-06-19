using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.UnitTests.Ingestion.Fakes;

public sealed class InMemoryIngestionCheckpointStore : IIngestionCheckpointStore
{
    private readonly Dictionary<CheckpointIdentity, IngestionCheckpoint> _checkpoints = new();

    public Task<IngestionCheckpoint?> GetAsync(CheckpointIdentity identity, CancellationToken ct)
    {
        _checkpoints.TryGetValue(identity, out var checkpoint);
        return Task.FromResult<IngestionCheckpoint?>(checkpoint);
    }

    public Task SaveAsync(CheckpointIdentity identity, IngestionCheckpoint checkpoint, CancellationToken ct)
    {
        _checkpoints[identity] = checkpoint;
        return Task.CompletedTask;
    }
}
