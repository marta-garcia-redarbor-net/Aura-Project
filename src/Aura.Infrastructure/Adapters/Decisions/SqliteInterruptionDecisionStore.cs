using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace Aura.Infrastructure.Adapters.Decisions;

/// <summary>
/// SQLite-backed store for interruption decisions (audit trail).
/// Uses the <c>InterruptionDecisions</c> table.
/// </summary>
internal sealed class SqliteInterruptionDecisionStore : IInterruptionDecisionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
                FocusState TEXT NOT NULL,
                RetrievedSemanticContext TEXT NULL,
                LlmRationale TEXT NULL,
                GuardrailOutcome TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_InterruptionDecisions_Timestamp
                ON InterruptionDecisions (Timestamp DESC);
            """;
        cmd.ExecuteNonQuery();

        EnsureColumn(connection, "InterruptionDecisions", "RetrievedSemanticContext", "TEXT");
        EnsureColumn(connection, "InterruptionDecisions", "LlmRationale", "TEXT");
        EnsureColumn(connection, "InterruptionDecisions", "GuardrailOutcome", "TEXT");
    }

    public Task RecordAsync(InterruptionDecisionRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO InterruptionDecisions (Id, WorkItemId, Title, SourceType, Decision, PriorityScore, Explanation, Timestamp, FocusState, RetrievedSemanticContext, LlmRationale, GuardrailOutcome)
            VALUES (@Id, @WorkItemId, @Title, @SourceType, @Decision, @PriorityScore, @Explanation, @Timestamp, @FocusState, @RetrievedSemanticContext, @LlmRationale, @GuardrailOutcome)
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
        cmd.Parameters.AddWithValue("@RetrievedSemanticContext", SerializeContext(record.RetrievedSemanticContext) ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@LlmRationale", (object?)record.LlmRationale ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@GuardrailOutcome", (object?)record.GuardrailOutcome ?? DBNull.Value);
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
            SELECT WorkItemId, Title, SourceType, Decision, PriorityScore, Explanation, Timestamp, FocusState, RetrievedSemanticContext, LlmRationale, GuardrailOutcome
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
            FocusState: reader.GetString(7),
            RetrievedSemanticContext: DeserializeContext(reader.IsDBNull(8) ? null : reader.GetString(8)),
            LlmRationale: reader.IsDBNull(9) ? null : reader.GetString(9),
            GuardrailOutcome: reader.IsDBNull(10) ? "confirmed" : reader.GetString(10));
    }

    private static string? SerializeContext(IReadOnlyList<DecisionContextItem>? context)
    {
        if (context is null || context.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(context, JsonOptions);
    }

    private static IReadOnlyList<DecisionContextItem> DeserializeContext(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<DecisionContextItem>>(value, JsonOptions) ?? [];
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM InterruptionDecisions";
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string typeDefinition)
    {
        using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = $"PRAGMA table_info({table});";

        using var reader = checkCmd.ExecuteReader();
        while (reader.Read())
        {
            var existingName = reader.GetString(1);
            if (string.Equals(existingName, column, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alterCmd = connection.CreateCommand();
        alterCmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {typeDefinition};";
        alterCmd.ExecuteNonQuery();
    }
}
