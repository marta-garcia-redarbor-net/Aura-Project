using Aura.Application.Models;
using Aura.Infrastructure.Adapters.Decisions;
using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aura.IntegrationTests.Adapters.Decisions;

public class EfDecisionStoreTraceTests
{
    [Fact]
    public async Task RecordAndQueryAsync_RoundTripsTraceFields()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aura-ef-trace-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        await using var db = new AuraDbContext(options);
        await db.Database.MigrateAsync();

        var store = new EfInterruptionDecisionStore(db);
        var record = new InterruptionDecisionRecord(
            WorkItemId: Guid.NewGuid(),
            Title: "EF trace item",
            SourceType: "outlook",
            Decision: "QUEUE",
            PriorityScore: 42,
            Explanation: "EF trace",
            Timestamp: DateTimeOffset.UtcNow,
            FocusState: "DeepWork",
            RetrievedSemanticContext:
            [
                new DecisionContextItem("ctx-ef-1", "Related thread context", "ActivityMemory", 0.77)
            ],
            LlmRationale: "LLM suggests queue",
            GuardrailOutcome: "confirmed");

        await store.RecordAsync(record, CancellationToken.None);
        var result = await store.QueryAsync(1, 10, CancellationToken.None);

        Assert.Single(result.Items);
        var persisted = result.Items[0];
        Assert.Equal("confirmed", persisted.GuardrailOutcome);
        Assert.Equal("LLM suggests queue", persisted.LlmRationale);
        Assert.Single(persisted.RetrievedSemanticContext!);
        Assert.Equal("ctx-ef-1", persisted.RetrievedSemanticContext![0].CanonicalSourceId);
    }
}
