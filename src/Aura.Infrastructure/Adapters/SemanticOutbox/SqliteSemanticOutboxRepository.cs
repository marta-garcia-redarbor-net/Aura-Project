using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.SemanticIndex.Enums;
using Microsoft.Data.Sqlite;

namespace Aura.Infrastructure.Adapters.Ingestion.SemanticOutbox;

/// <summary>
/// SQLite-backed outbox repository for semantic index sync entries.
/// V1: semantic-specific outbox; generalizable later.
/// </summary>
public sealed class SqliteSemanticOutboxRepository : ISemanticOutboxRepository
{
    private readonly SqliteConnection _connection;

    public SqliteSemanticOutboxRepository(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>Creates the outbox table if it doesn't exist.</summary>
    public static void InitializeSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS SemanticOutbox (
                Id TEXT PRIMARY KEY,
                CanonicalSourceId TEXT NOT NULL,
                Content TEXT NOT NULL,
                Collection INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                Processed INTEGER NOT NULL DEFAULT 0,
                ProcessedAt TEXT NULL,
                Error TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_SemanticOutbox_Pending
                ON SemanticOutbox (Processed, CreatedAt)
                WHERE Processed = 0;
            """;
        cmd.ExecuteNonQuery();
    }

    public Task EnqueueAsync(SemanticOutboxEntry entry, CancellationToken ct)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO SemanticOutbox (Id, CanonicalSourceId, Content, Collection, CreatedAt, Processed, ProcessedAt, Error)
            VALUES (@Id, @CanonicalSourceId, @Content, @Collection, @CreatedAt, @Processed, @ProcessedAt, @Error);
            """;
        cmd.Parameters.AddWithValue("@Id", entry.Id.ToString());
        cmd.Parameters.AddWithValue("@CanonicalSourceId", entry.CanonicalSourceId);
        cmd.Parameters.AddWithValue("@Content", entry.Content);
        cmd.Parameters.AddWithValue("@Collection", (int)entry.Collection);
        cmd.Parameters.AddWithValue("@CreatedAt", entry.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@Processed", entry.Processed ? 1 : 0);
        cmd.Parameters.AddWithValue("@ProcessedAt", (object?)entry.ProcessedAt?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Error", (object?)entry.Error ?? DBNull.Value);
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SemanticOutboxEntry>> FetchPendingAsync(int batchSize, CancellationToken ct)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, CanonicalSourceId, Content, Collection, CreatedAt, Processed, ProcessedAt, Error
            FROM SemanticOutbox
            WHERE Processed = 0
            ORDER BY CreatedAt ASC
            LIMIT @BatchSize;
            """;
        cmd.Parameters.AddWithValue("@BatchSize", batchSize);

        var results = new List<SemanticOutboxEntry>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(ReadEntry(reader));
        }

        return Task.FromResult<IReadOnlyList<SemanticOutboxEntry>>(results);
    }

    public Task UpdateAsync(SemanticOutboxEntry entry, CancellationToken ct)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE SemanticOutbox
            SET Processed = @Processed, ProcessedAt = @ProcessedAt, Error = @Error
            WHERE Id = @Id;
            """;
        cmd.Parameters.AddWithValue("@Id", entry.Id.ToString());
        cmd.Parameters.AddWithValue("@Processed", entry.Processed ? 1 : 0);
        cmd.Parameters.AddWithValue("@ProcessedAt", (object?)entry.ProcessedAt?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Error", (object?)entry.Error ?? DBNull.Value);
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private static SemanticOutboxEntry ReadEntry(SqliteDataReader reader)
    {
        var id = Guid.Parse(reader.GetString(0));
        var canonicalSourceId = reader.GetString(1);
        var content = reader.GetString(2);
        var collection = (SemanticCollectionType)reader.GetInt32(3);
        var createdAt = DateTimeOffset.Parse(reader.GetString(4));

        var entry = new SemanticOutboxEntry(id, canonicalSourceId, content, collection, createdAt);

        var processed = reader.GetInt32(5) == 1;
        if (processed)
        {
            entry.MarkProcessed();
        }

        if (!reader.IsDBNull(7))
        {
            entry.MarkFailed(reader.GetString(7));
        }

        return entry;
    }
}
