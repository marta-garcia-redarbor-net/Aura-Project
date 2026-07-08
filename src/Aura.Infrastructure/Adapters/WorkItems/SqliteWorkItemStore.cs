using System.Text.Json;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.Data.Sqlite;

namespace Aura.Infrastructure.Adapters.WorkItems;

/// <summary>
/// SQLite-backed work item store with idempotent upsert on ExternalId.
/// Also provides read-back capability via <see cref="IWorkItemReader"/>.
/// </summary>
internal sealed class SqliteWorkItemStore : IWorkItemStore, IWorkItemReader
{
    private readonly SqliteConnection _connection;

    public SqliteWorkItemStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>Creates the WorkItems table if it doesn't exist.</summary>
    public static void InitializeSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS WorkItems (
                Id TEXT PRIMARY KEY,
                ExternalId TEXT NOT NULL UNIQUE,
                Title TEXT NOT NULL,
                Source TEXT NOT NULL,
                SourceType TEXT NOT NULL,
                Priority TEXT NOT NULL,
                MetadataJson TEXT NOT NULL,
                CorrelationId TEXT NOT NULL,
                CapturedAtUtc TEXT NOT NULL,
                SchemaVersion TEXT NOT NULL,
                Status TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NULL,
                FaultReason TEXT NULL,
                PriorityScore INTEGER NULL,
                OwnerUserId TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_WorkItems_ExternalId
                ON WorkItems (ExternalId);
            CREATE INDEX IF NOT EXISTS IX_WorkItems_CapturedAtUtc
                ON WorkItems (CapturedAtUtc);
            """;
        cmd.ExecuteNonQuery();

        // Migration: add UpdatedAt column for existing databases (safe no-op if already present)
        try
        {
            using var migrateCmd = connection.CreateCommand();
            migrateCmd.CommandText = "ALTER TABLE WorkItems ADD COLUMN UpdatedAt TEXT;";
            migrateCmd.ExecuteNonQuery();
        }
        catch
        {
            // Column already exists — ignore
        }

        // Migration: add PriorityScore column (W3-H3)
        try
        {
            using var migrateCmd = connection.CreateCommand();
            migrateCmd.CommandText = "ALTER TABLE WorkItems ADD COLUMN PriorityScore INTEGER NULL;";
            migrateCmd.ExecuteNonQuery();
        }
        catch
        {
            // Column already exists — ignore
        }

        // Migration: add OwnerUserId column
        try
        {
            using var migrateCmd = connection.CreateCommand();
            migrateCmd.CommandText = "ALTER TABLE WorkItems ADD COLUMN OwnerUserId TEXT NULL;";
            migrateCmd.ExecuteNonQuery();
        }
        catch
        {
            // Column already exists — ignore
        }
    }

    public Task<WorkItem?> FindByExternalIdAsync(string externalId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(externalId);
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT ExternalId, Title, Source, SourceType, Priority, MetadataJson, CorrelationId, CapturedAtUtc, PriorityScore, OwnerUserId
            FROM WorkItems
            WHERE ExternalId = @ExternalId
            """;
        cmd.Parameters.AddWithValue("@ExternalId", externalId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return Task.FromResult<WorkItem?>(null);

        var result = ReadWorkItemFromReader(reader);
        return Task.FromResult<WorkItem?>(result);
    }

    public Task<WorkItemPersistenceResult> SaveAsync(WorkItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO WorkItems (Id, ExternalId, Title, Source, SourceType, Priority, MetadataJson,
                                   CorrelationId, CapturedAtUtc, SchemaVersion, Status, CreatedAt, 
                                   FaultReason, PriorityScore, OwnerUserId)
            VALUES (@Id, @ExternalId, @Title, @Source, @SourceType, @Priority, @MetadataJson,
                    @CorrelationId, @CapturedAtUtc, @SchemaVersion, @Status, @CreatedAt, 
                    @FaultReason, @PriorityScore, @OwnerUserId)
            ON CONFLICT(ExternalId) DO UPDATE SET
                Title = excluded.Title,
                Source = excluded.Source,
                SourceType = excluded.SourceType,
                MetadataJson = excluded.MetadataJson,
                CapturedAtUtc = excluded.CapturedAtUtc,
                Status = excluded.Status,
                FaultReason = excluded.FaultReason,
                PriorityScore = excluded.PriorityScore,
                OwnerUserId = excluded.OwnerUserId;
            """;
        cmd.Parameters.AddWithValue("@Id", item.Id.ToString());
        cmd.Parameters.AddWithValue("@ExternalId", item.ExternalId);
        cmd.Parameters.AddWithValue("@Title", item.Title);
        cmd.Parameters.AddWithValue("@Source", item.Source);
        cmd.Parameters.AddWithValue("@SourceType", item.SourceType.ToString());
        cmd.Parameters.AddWithValue("@Priority", item.Priority.ToString());
        cmd.Parameters.AddWithValue("@MetadataJson", JsonSerializer.Serialize(item.Metadata));
        cmd.Parameters.AddWithValue("@CorrelationId", item.CorrelationId);
        cmd.Parameters.AddWithValue("@CapturedAtUtc", item.CapturedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@SchemaVersion", item.SchemaVersion);
        cmd.Parameters.AddWithValue("@Status", item.Status.ToString());
        cmd.Parameters.AddWithValue("@CreatedAt", item.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@FaultReason", (object?)item.FaultReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@PriorityScore", (object?)item.PriorityScore ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@OwnerUserId", (object?)item.OwnerUserId ?? DBNull.Value);
        cmd.ExecuteNonQuery();

        return Task.FromResult(WorkItemPersistenceResult.Success());
    }

    public Task<IReadOnlyList<WorkItem>> ReadBySourceAsync(
        WorkItemSourceType sourceType,
        WorkItemStatus? statusFilter,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, ExternalId, Title, Source, SourceType, Priority, MetadataJson,
                   CorrelationId, CapturedAtUtc, SchemaVersion, Status, CreatedAt, FaultReason, PriorityScore, OwnerUserId
            FROM WorkItems
            WHERE SourceType = @SourceType
              AND (@Status IS NULL OR Status = @Status)
            ORDER BY
              COALESCE(PriorityScore,
                CASE Priority
                  WHEN 'Critical' THEN 100
                  WHEN 'High' THEN 75
                  WHEN 'Medium' THEN 50
                  WHEN 'Low' THEN 25
                  ELSE 0
                END
              ) DESC,
              CapturedAtUtc DESC;
            """;
        cmd.Parameters.AddWithValue("@SourceType", sourceType.ToString());
        cmd.Parameters.AddWithValue("@Status", (object?)statusFilter?.ToString() ?? DBNull.Value);

        var items = new List<WorkItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = Guid.Parse(reader.GetString(0));
            var externalId = reader.GetString(1);
            var title = reader.GetString(2);
            var source = reader.GetString(3);
            var sourceTypeValue = Enum.Parse<WorkItemSourceType>(reader.GetString(4));
            var priority = Enum.Parse<WorkItemPriority>(reader.GetString(5));
            var metadataJson = reader.GetString(6);
            var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson)
                           ?? new Dictionary<string, string>();
            var correlationId = reader.GetString(7);
            var capturedAtUtc = DateTimeOffset.Parse(reader.GetString(8));
            int? priorityScore = reader.IsDBNull(13) ? null : reader.GetInt32(13);

            items.Add(new WorkItem(externalId, title, source, sourceTypeValue, priority, metadata, correlationId, capturedAtUtc, priorityScore: priorityScore));
        }

        return Task.FromResult<IReadOnlyList<WorkItem>>(items);
    }

    public Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(MorningSummaryQuery query, CancellationToken cancellationToken)
        => ReadForWindowAsync(query, null, cancellationToken);

    public Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
        MorningSummaryQuery query, WorkItemStatus? statusFilter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();

        var whereClause = "CapturedAtUtc >= @FromUtc AND CapturedAtUtc <= @ToUtc";
        if (statusFilter.HasValue)
        {
            whereClause += " AND Status = @Status";
            cmd.Parameters.AddWithValue("@Status", statusFilter.Value.ToString());
        }

        // Filter by OwnerUserId: only items owned by the query user, or items visible to all
        if (!string.IsNullOrEmpty(query.UserId))
        {
            whereClause += " AND (OwnerUserId IS NULL OR OwnerUserId = @UserId)";
            cmd.Parameters.AddWithValue("@UserId", query.UserId);
        }

        cmd.CommandText = $"""
            SELECT ExternalId, Title, Source, SourceType, Priority, MetadataJson, CorrelationId, CapturedAtUtc, PriorityScore, OwnerUserId
            FROM WorkItems
            WHERE {whereClause}
            ORDER BY CapturedAtUtc DESC;
            """;
        cmd.Parameters.AddWithValue("@FromUtc", query.FromUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@ToUtc", query.ToUtc.ToString("O"));

        var items = new List<WorkItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(ReadWorkItemFromReader(reader));
        }

        return Task.FromResult<IReadOnlyList<WorkItem>>(items);
    }

    public Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
        WorkItemSourceType sourceType, MorningSummaryQuery query, WorkItemStatus? statusFilter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();

        var filters = new List<string> { "CapturedAtUtc >= @FromUtc AND CapturedAtUtc <= @ToUtc", "SourceType = @SourceType" };
        cmd.Parameters.AddWithValue("@SourceType", sourceType.ToString());
        if (statusFilter.HasValue)
        {
            filters.Add("Status = @Status");
            cmd.Parameters.AddWithValue("@Status", statusFilter.Value.ToString());
        }

        cmd.CommandText = $"""
            SELECT ExternalId, Title, Source, SourceType, Priority, MetadataJson, CorrelationId, CapturedAtUtc, PriorityScore
            FROM WorkItems
            WHERE {string.Join(" AND ", filters)}
            ORDER BY CapturedAtUtc DESC;
            """;
        cmd.Parameters.AddWithValue("@FromUtc", query.FromUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@ToUtc", query.ToUtc.ToString("O"));

        var items = new List<WorkItem>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(ReadWorkItemFromReader(reader));
        }

        return Task.FromResult<IReadOnlyList<WorkItem>>(items);
    }

    public Task<IReadOnlySet<string>> GetPendingExternalIdsAsync(WorkItemSourceType source, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT ExternalId FROM WorkItems
            WHERE Status = @Status AND SourceType = @SourceType
            """;
        cmd.Parameters.AddWithValue("@Status", "Pending");
        cmd.Parameters.AddWithValue("@SourceType", source.ToString());

        var ids = new HashSet<string>(StringComparer.Ordinal);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ids.Add(reader.GetString(0));
        }

        return Task.FromResult<IReadOnlySet<string>>(ids);
    }

    public Task MarkCompletedAsync(IReadOnlySet<string> externalIds, WorkItemSourceType source, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(externalIds);
        ct.ThrowIfCancellationRequested();

        if (externalIds.Count == 0)
            return Task.CompletedTask;

        using var cmd = _connection.CreateCommand();
        var now = DateTimeOffset.UtcNow.ToString("O");

        // Build parameterized IN clause dynamically
        var paramNames = new List<string>();
        var index = 0;
        foreach (var id in externalIds)
        {
            var paramName = $"@id{index}";
            paramNames.Add(paramName);
            cmd.Parameters.AddWithValue(paramName, id);
            index++;
        }

        cmd.CommandText = $"""
            UPDATE WorkItems
            SET Status = @CompletedStatus, UpdatedAt = @Now
            WHERE Status = @PendingStatus
              AND SourceType = @SourceType
              AND ExternalId IN ({string.Join(", ", paramNames)})
            """;
        cmd.Parameters.AddWithValue("@CompletedStatus", "Completed");
        cmd.Parameters.AddWithValue("@PendingStatus", "Pending");
        cmd.Parameters.AddWithValue("@Now", now);
        cmd.Parameters.AddWithValue("@SourceType", source.ToString());

        cmd.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    /// <summary>Reads a single WorkItem from the current row of a SqliteDataReader.
    /// Assumes columns: 0=ExternalId, 1=Title, 2=Source, 3=SourceType, 4=Priority,
    /// 5=MetadataJson, 6=CorrelationId, 7=CapturedAtUtc, 8=PriorityScore (nullable), 9=OwnerUserId (nullable).</summary>
    private static WorkItem ReadWorkItemFromReader(SqliteDataReader reader)
    {
        var externalId = reader.GetString(0);
        var title = reader.GetString(1);
        var source = reader.GetString(2);
        var sourceType = Enum.Parse<WorkItemSourceType>(reader.GetString(3));
        var priority = Enum.Parse<WorkItemPriority>(reader.GetString(4));
        var metadataJson = reader.GetString(5);
        var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson)
                       ?? new Dictionary<string, string>();
        var correlationId = reader.GetString(6);
        var capturedAtUtc = DateTimeOffset.Parse(reader.GetString(7));
        int? priorityScore = reader.IsDBNull(8) ? null : reader.GetInt32(8);
        string? ownerUserId = reader.IsDBNull(9) ? null : reader.GetString(9);

        return new WorkItem(externalId, title, source, sourceType, priority, metadata, correlationId, capturedAtUtc, priorityScore: priorityScore, ownerUserId: ownerUserId);
    }
}
