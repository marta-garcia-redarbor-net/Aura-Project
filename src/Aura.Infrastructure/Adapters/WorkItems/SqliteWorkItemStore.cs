using System.Text.Json;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.Data.Sqlite;

namespace Aura.Infrastructure.Adapters.WorkItems;

/// <summary>
/// SQLite-backed work item store with idempotent upsert on ExternalId.
/// </summary>
internal sealed class SqliteWorkItemStore : IWorkItemStore
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
                FaultReason TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_WorkItems_ExternalId
                ON WorkItems (ExternalId);
            """;
        cmd.ExecuteNonQuery();
    }

    public Task<WorkItemPersistenceResult> SaveAsync(WorkItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO WorkItems (Id, ExternalId, Title, Source, SourceType, Priority, MetadataJson,
                                   CorrelationId, CapturedAtUtc, SchemaVersion, Status, CreatedAt, FaultReason)
            VALUES (@Id, @ExternalId, @Title, @Source, @SourceType, @Priority, @MetadataJson,
                    @CorrelationId, @CapturedAtUtc, @SchemaVersion, @Status, @CreatedAt, @FaultReason)
            ON CONFLICT(ExternalId) DO UPDATE SET
                Title = excluded.Title,
                Source = excluded.Source,
                SourceType = excluded.SourceType,
                Priority = excluded.Priority,
                MetadataJson = excluded.MetadataJson,
                CapturedAtUtc = excluded.CapturedAtUtc,
                Status = excluded.Status,
                FaultReason = excluded.FaultReason;
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
        cmd.ExecuteNonQuery();

        return Task.FromResult(WorkItemPersistenceResult.Success());
    }
}
