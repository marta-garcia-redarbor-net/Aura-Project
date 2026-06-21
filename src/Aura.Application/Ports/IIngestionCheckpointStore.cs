using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Port for ingestion checkpoint persistence.
/// When <see cref="GetAsync"/> returns null (no prior checkpoint), callers MUST
/// bound the initial data fetch to the UTC-today window (00:00:00 → UtcNow).
/// This window is caller behavior and MUST NOT be stored in checkpoint fields.
/// Implementation lives in Infrastructure — never reference provider SDK types here.
/// </summary>
public interface IIngestionCheckpointStore
{
    /// <summary>Returns the stored checkpoint for the given identity, or null if none exists.</summary>
    Task<IngestionCheckpoint?> GetAsync(CheckpointIdentity identity, CancellationToken ct);

    /// <summary>Writes or replaces the checkpoint for the given identity.</summary>
    Task SaveAsync(CheckpointIdentity identity, IngestionCheckpoint checkpoint, CancellationToken ct);
}
