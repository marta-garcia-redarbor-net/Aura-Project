using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.Data.Sqlite;

namespace Aura.Infrastructure.Adapters.Notifications;

/// <summary>
/// SQLite-backed outbox for cross-process notification entries.
/// </summary>
internal sealed class SqliteNotificationOutboxStore : INotificationOutboxStore
{
    private readonly SqliteConnection _connection;

    public SqliteNotificationOutboxStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public static void InitializeSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS NotificationOutbox (
                Id TEXT PRIMARY KEY,
                WorkItemId TEXT NOT NULL,
                UserId TEXT NOT NULL,
                SourceType TEXT NOT NULL,
                Title TEXT NOT NULL,
                Priority REAL NOT NULL,
                TriggerRule TEXT NULL,
                CreatedAt TEXT NOT NULL,
                DispatchedAt TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_NotificationOutbox_Pending
                ON NotificationOutbox (DispatchedAt, Priority DESC, CreatedAt ASC);
            """;
        cmd.ExecuteNonQuery();
    }

    public Task EnqueueAsync(NotificationOutboxEntry entry, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO NotificationOutbox (Id, WorkItemId, UserId, SourceType, Title, Priority, TriggerRule, CreatedAt, DispatchedAt)
            VALUES (@Id, @WorkItemId, @UserId, @SourceType, @Title, @Priority, @TriggerRule, @CreatedAt, @DispatchedAt)
            """;
        cmd.Parameters.AddWithValue("@Id", entry.Id.ToString());
        cmd.Parameters.AddWithValue("@WorkItemId", entry.WorkItemId.ToString());
        cmd.Parameters.AddWithValue("@UserId", entry.UserId);
        cmd.Parameters.AddWithValue("@SourceType", entry.SourceType);
        cmd.Parameters.AddWithValue("@Title", entry.Title);
        cmd.Parameters.AddWithValue("@Priority", entry.Priority);
        cmd.Parameters.AddWithValue("@TriggerRule", (object?)entry.TriggerRule ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", entry.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@DispatchedAt", DBNull.Value);
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NotificationOutboxEntry>> GetPendingAsync(int limit, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, WorkItemId, UserId, SourceType, Title, Priority, TriggerRule, CreatedAt, DispatchedAt
            FROM NotificationOutbox
            WHERE DispatchedAt IS NULL
            ORDER BY Priority DESC, CreatedAt ASC
            LIMIT @Limit
            """;
        cmd.Parameters.AddWithValue("@Limit", limit);

        var entries = new List<NotificationOutboxEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(ReadEntryFromReader(reader));
        }

        return Task.FromResult<IReadOnlyList<NotificationOutboxEntry>>(entries);
    }

    public Task MarkDispatchedAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE NotificationOutbox
            SET DispatchedAt = @DispatchedAt
            WHERE Id = @Id
            """;
        cmd.Parameters.AddWithValue("@Id", id.ToString());
        cmd.Parameters.AddWithValue("@DispatchedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private static NotificationOutboxEntry ReadEntryFromReader(SqliteDataReader reader)
    {
        var dispatchedAt = reader.IsDBNull(8) ? null : (DateTimeOffset?)DateTimeOffset.Parse(reader.GetString(8));

        // Use InternalsVisibleTo or the internal constructor
        return new NotificationOutboxEntry(
            id: Guid.Parse(reader.GetString(0)),
            workItemId: Guid.Parse(reader.GetString(1)),
            userId: reader.GetString(2),
            sourceType: reader.GetString(3),
            title: reader.GetString(4),
            priority: reader.GetDouble(5),
            triggerRule: reader.IsDBNull(6) ? null : reader.GetString(6),
            createdAt: DateTimeOffset.Parse(reader.GetString(7)),
            dispatchedAt: dispatchedAt);
    }
}
