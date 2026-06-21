using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.UnitTests.Ingestion.Support;

public sealed class IngestionCheckpointCallerHarness
{
    private readonly IIngestionCheckpointStore _store;
    private readonly Func<DateTimeOffset> _utcNow;

    public IngestionCheckpointCallerHarness(IIngestionCheckpointStore store, Func<DateTimeOffset> utcNow)
    {
        _store = store;
        _utcNow = utcNow;
    }

    public async Task<IngestionFetchPlan> ResolveFetchPlanAsync(CheckpointIdentity identity, CancellationToken ct)
    {
        var checkpoint = await _store.GetAsync(identity, ct);
        if (checkpoint is not null)
        {
            return new IngestionFetchPlan(checkpoint, null, null);
        }

        var now = _utcNow();
        var start = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        return new IngestionFetchPlan(null, start, now);
    }
}

public sealed record IngestionFetchPlan(
    IngestionCheckpoint? Checkpoint,
    DateTimeOffset? WindowStartUtc,
    DateTimeOffset? WindowEndUtc);
