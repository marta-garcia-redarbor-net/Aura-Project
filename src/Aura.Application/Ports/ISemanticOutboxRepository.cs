namespace Aura.Application.Ports;

/// <summary>
/// Port for outbox persistence — enqueue and fetch pending semantic sync entries.
/// Implementation lives in Infrastructure (SQLite for V1).
/// </summary>
public interface ISemanticOutboxRepository
{
    /// <summary>Enqueues a new entry for later processing by the sync worker.</summary>
    Task EnqueueAsync(Models.SemanticOutboxEntry entry, CancellationToken ct);

    /// <summary>Returns unprocessed entries ordered by creation time, up to <paramref name="batchSize"/>.</summary>
    Task<IReadOnlyList<Models.SemanticOutboxEntry>> FetchPendingAsync(int batchSize, CancellationToken ct);

    /// <summary>Persists the updated state of an entry (processed/failed).</summary>
    Task UpdateAsync(Models.SemanticOutboxEntry entry, CancellationToken ct);
}
