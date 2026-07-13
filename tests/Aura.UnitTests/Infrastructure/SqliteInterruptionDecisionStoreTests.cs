using Aura.Application.Models;
using Aura.Infrastructure.Adapters.Decisions;
using Microsoft.Data.Sqlite;

namespace Aura.UnitTests.Infrastructure;

public class SqliteInterruptionDecisionStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteInterruptionDecisionStore _store;

    public SqliteInterruptionDecisionStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SqliteInterruptionDecisionStore.InitializeSchema(_connection);
        _store = new SqliteInterruptionDecisionStore(_connection);
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    private static InterruptionDecisionRecord CreateRecord(string verdict, int? score = 88, string focusState = "WindowOfOpportunity")
    {
        return new InterruptionDecisionRecord(
            WorkItemId: Guid.NewGuid(),
            Title: "Test item",
            SourceType: "email",
            Decision: verdict,
            PriorityScore: score,
            Explanation: $"Test {verdict}",
            Timestamp: DateTimeOffset.UtcNow,
            FocusState: focusState);
    }

    [Fact]
    public async Task RecordAsync_ThenQueryAsync_ReturnsRecord()
    {
        var record = CreateRecord("INTERRUPT");

        await _store.RecordAsync(record, CancellationToken.None);
        var result = await _store.QueryAsync(1, 20, cancellationToken: CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(record.WorkItemId, result.Items[0].WorkItemId);
        Assert.Equal(record.Title, result.Items[0].Title);
        Assert.Equal(record.Decision, result.Items[0].Decision);
        Assert.Equal(record.PriorityScore, result.Items[0].PriorityScore);
        Assert.Equal(record.Explanation, result.Items[0].Explanation);
        Assert.Equal(record.FocusState, result.Items[0].FocusState);
    }

    [Fact]
    public async Task QueryAsync_WhenEmpty_ReturnsEmptyResult()
    {
        var result = await _store.QueryAsync(1, 20, cancellationToken: CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task QueryAsync_ReturnsRecordsSortedByTimestampDesc()
    {
        var early = CreateRecord("QUEUE");
        var mid = CreateRecord("DEFER");
        var late = CreateRecord("INTERRUPT");

        // Insert out of order
        await _store.RecordAsync(mid with { Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5) }, CancellationToken.None);
        await _store.RecordAsync(late with { Timestamp = DateTimeOffset.UtcNow }, CancellationToken.None);
        await _store.RecordAsync(early with { Timestamp = DateTimeOffset.UtcNow.AddMinutes(-10) }, CancellationToken.None);

        var result = await _store.QueryAsync(1, 20, cancellationToken: CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal("INTERRUPT", result.Items[0].Decision);
        Assert.Equal("QUEUE", result.Items[2].Decision);
    }

    [Fact]
    public async Task QueryAsync_Pagination_ReturnsCorrectPage()
    {
        for (int i = 0; i < 5; i++)
        {
            await _store.RecordAsync(
                CreateRecord("QUEUE", score: i) with { Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i) },
                CancellationToken.None);
        }

        var page1 = await _store.QueryAsync(1, 2, cancellationToken: CancellationToken.None);
        var page2 = await _store.QueryAsync(2, 2, cancellationToken: CancellationToken.None);

        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(5, page2.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(3, page2.TotalPages);
    }

    [Fact]
    public async Task QueryAsync_WhenPageIsLessThanOne_NormalizesToFirstPage()
    {
        for (int i = 0; i < 3; i++)
        {
            await _store.RecordAsync(
                CreateRecord("QUEUE", score: i) with { Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i) },
                CancellationToken.None);
        }

        var result = await _store.QueryAsync(0, 2, cancellationToken: CancellationToken.None);

        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task QueryAsync_WhenPageSizeIsLessThanOne_UsesDefaultPageSizeTwenty()
    {
        await _store.RecordAsync(CreateRecord("INTERRUPT"), CancellationToken.None);

        var result = await _store.QueryAsync(1, 0, cancellationToken: CancellationToken.None);

        Assert.Equal(20, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task QueryAsync_WhenPageExceedsTotalPages_ReturnsEmptyItemsWithTotalCount()
    {
        for (int i = 0; i < 3; i++)
        {
            await _store.RecordAsync(
                CreateRecord("DEFER", score: i) with { Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i) },
                CancellationToken.None);
        }

        var result = await _store.QueryAsync(3, 2, cancellationToken: CancellationToken.None);

        Assert.Equal(3, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task RecordAsync_StoresNullPriorityScore()
    {
        var record = CreateRecord("DEFER", score: null);

        await _store.RecordAsync(record, CancellationToken.None);
        var result = await _store.QueryAsync(1, 20, cancellationToken: CancellationToken.None);

        Assert.Null(result.Items[0].PriorityScore);
    }

    [Fact]
    public async Task RecordAsync_StoresAllVerdictTypes()
    {
        await _store.RecordAsync(CreateRecord("INTERRUPT"), CancellationToken.None);
        await _store.RecordAsync(CreateRecord("QUEUE"), CancellationToken.None);
        await _store.RecordAsync(CreateRecord("DEFER"), CancellationToken.None);

        var result = await _store.QueryAsync(1, 20, cancellationToken: CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task RecordAsync_ThenQueryAsync_RoundTripsTraceFields()
    {
        var context = new List<DecisionContextItem>
        {
            new("ctx-1", "Production outage context", "ActivityMemory", 0.93)
        };

        var record = new InterruptionDecisionRecord(
            WorkItemId: Guid.NewGuid(),
            Title: "Trace test item",
            SourceType: "teams",
            Decision: "INTERRUPT",
            PriorityScore: 95,
            Explanation: "Trace round-trip",
            Timestamp: DateTimeOffset.UtcNow,
            FocusState: "WindowOfOpportunity",
            RetrievedSemanticContext: context,
            LlmRationale: "LLM rationale text",
            GuardrailOutcome: "adjusted");

        await _store.RecordAsync(record, CancellationToken.None);
        var result = await _store.QueryAsync(1, 20, cancellationToken: CancellationToken.None);

        Assert.Single(result.Items);
        var persisted = result.Items[0];
        Assert.Equal("adjusted", persisted.GuardrailOutcome);
        Assert.Equal("LLM rationale text", persisted.LlmRationale);
        Assert.Single(persisted.RetrievedSemanticContext!);
        Assert.Equal("ctx-1", persisted.RetrievedSemanticContext![0].CanonicalSourceId);
    }
}
