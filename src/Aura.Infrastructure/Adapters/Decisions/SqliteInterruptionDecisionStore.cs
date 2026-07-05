using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Data.Sqlite;

namespace Aura.Infrastructure.Adapters.Decisions;

/// <summary>
/// SQLite-backed store for interruption decisions (audit trail).
/// Uses the <c>InterruptionDecisions</c> table.
/// </summary>
internal sealed class SqliteInterruptionDecisionStore : IInterruptionDecisionStore
{
    private readonly SqliteConnection _connection;

    public SqliteInterruptionDecisionStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public static void InitializeSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS InterruptionDecisions (
                Id TEXT PRIMARY KEY,
                WorkItemId TEXT NOT NULL,
                Title TEXT NOT NULL,
                SourceType TEXT NOT NULL,
                Decision TEXT NOT NULL,
                PriorityScore INTEGER NULL,
                Explanation TEXT NULL,
                Timestamp TEXT NOT NULL,
                FocusState TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_InterruptionDecisions_Timestamp
                ON InterruptionDecisions (Timestamp DESC);
            """;
        cmd.ExecuteNonQuery();
    }

    public Task RecordAsync(InterruptionDecisionRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO InterruptionDecisions (Id, WorkItemId, Title, SourceType, Decision, PriorityScore, Explanation, Timestamp, FocusState)
            VALUES (@Id, @WorkItemId, @Title, @SourceType, @Decision, @PriorityScore, @Explanation, @Timestamp, @FocusState)
            """;
        cmd.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
        cmd.Parameters.AddWithValue("@WorkItemId", record.WorkItemId.ToString());
        cmd.Parameters.AddWithValue("@Title", record.Title);
        cmd.Parameters.AddWithValue("@SourceType", record.SourceType);
        cmd.Parameters.AddWithValue("@Decision", record.Decision);
        cmd.Parameters.AddWithValue("@PriorityScore", (object?)record.PriorityScore ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Explanation", record.Explanation);
        cmd.Parameters.AddWithValue("@Timestamp", record.Timestamp.ToString("O"));
        cmd.Parameters.AddWithValue("@FocusState", record.FocusState);
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<PagedResult<InterruptionDecisionRecord>> QueryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var offset = (page - 1) * pageSize;

        using var countCmd = _connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM InterruptionDecisions";
        var totalCount = Convert.ToInt32(countCmd.ExecuteScalar());

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT WorkItemId, Title, SourceType, Decision, PriorityScore, Explanation, Timestamp, FocusState
            FROM InterruptionDecisions
            ORDER BY Timestamp DESC
            LIMIT @Limit OFFSET @Offset
            """;
        cmd.Parameters.AddWithValue("@Limit", pageSize);
        cmd.Parameters.AddWithValue("@Offset", offset);

        var items = new List<InterruptionDecisionRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(ReadRecordFromReader(reader));
        }

        var result = new PagedResult<InterruptionDecisionRecord>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Task.FromResult(result);
    }

    private static InterruptionDecisionRecord ReadRecordFromReader(SqliteDataReader reader)
    {
        return new InterruptionDecisionRecord(
            WorkItemId: Guid.Parse(reader.GetString(0)),
            Title: reader.GetString(1),
            SourceType: reader.GetString(2),
            Decision: reader.GetString(3),
            PriorityScore: reader.IsDBNull(4) ? null : reader.GetInt32(4),
            Explanation: reader.GetString(5),
            Timestamp: DateTimeOffset.Parse(reader.GetString(6)),
            FocusState: reader.GetString(7));
    }
}
