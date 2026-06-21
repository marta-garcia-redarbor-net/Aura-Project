using Aura.Application.Models;
using Aura.UnitTests.Ingestion.Fakes;
using Aura.UnitTests.Ingestion.Support;
using System.Globalization;

namespace Aura.UnitTests.Ingestion;

public class IngestionCheckpointFirstRunWindowTests
{
    [Fact]
    public async Task ResolveFetchPlanAsync_AppliesUtcTodayWindow_WhenCheckpointIsMissing()
    {
        var now = DateTimeOffset.Parse("2026-06-19T15:45:00Z", CultureInfo.InvariantCulture);
        var utcNow = () => now;
        var store = new InMemoryIngestionCheckpointStore();
        var harness = new IngestionCheckpointCallerHarness(store, utcNow);
        var identity = new CheckpointIdentity("teams", "messages", "acme");

        var plan = await harness.ResolveFetchPlanAsync(identity, CancellationToken.None);

        Assert.Null(plan.Checkpoint);
        Assert.Equal(DateTimeOffset.Parse("2026-06-19T00:00:00Z", CultureInfo.InvariantCulture), plan.WindowStartUtc);
        Assert.Equal(now, plan.WindowEndUtc);
    }

    [Fact]
    public async Task ResolveFetchPlanAsync_BypassesUtcTodayWindow_WhenCheckpointExists()
    {
        var now = DateTimeOffset.Parse("2026-06-19T15:45:00Z", CultureInfo.InvariantCulture);
        var utcNow = () => now;
        var store = new InMemoryIngestionCheckpointStore();
        var harness = new IngestionCheckpointCallerHarness(store, utcNow);
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpoint = new IngestionCheckpoint(
            "delta-v2",
            DateTimeOffset.Parse("2026-06-19T10:00:00Z", CultureInfo.InvariantCulture),
            DateTimeOffset.Parse("2026-06-19T10:10:00Z", CultureInfo.InvariantCulture));

        await store.SaveAsync(identity, checkpoint, CancellationToken.None);

        var plan = await harness.ResolveFetchPlanAsync(identity, CancellationToken.None);

        Assert.Equal(checkpoint, plan.Checkpoint);
        Assert.Null(plan.WindowStartUtc);
        Assert.Null(plan.WindowEndUtc);
    }
}
