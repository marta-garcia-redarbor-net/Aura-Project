using Aura.Application.Ports;
using Microsoft.Data.Sqlite;

namespace Aura.Infrastructure.Adapters.Rules;

/// <summary>
/// SQLite-backed store for dynamically managed alert rules (VIP senders and keywords).
/// Uses separate tables for VipSenders and AlertKeywords.
/// </summary>
internal sealed class SqliteAlertRuleStore : IAlertRuleStore
{
    private readonly SqliteConnection _connection;

    public SqliteAlertRuleStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public static void InitializeSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS VipSenders (
                Email TEXT PRIMARY KEY,
                AddedBy TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS AlertKeywords (
                Keyword TEXT PRIMARY KEY,
                AddedBy TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public Task<IReadOnlyList<string>> GetVipSendersAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Email FROM VipSenders ORDER BY Email";
        var senders = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            senders.Add(reader.GetString(0));
        }

        return Task.FromResult<IReadOnlyList<string>>(senders);
    }

    public Task<IReadOnlyList<string>> GetKeywordsAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Keyword FROM AlertKeywords ORDER BY Keyword";
        var keywords = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            keywords.Add(reader.GetString(0));
        }

        return Task.FromResult<IReadOnlyList<string>>(keywords);
    }

    public Task AddVipSenderAsync(string email, string addedBy, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO VipSenders (Email, AddedBy, CreatedAt)
            VALUES (@Email, @AddedBy, @CreatedAt)
            """;
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@AddedBy", addedBy);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task RemoveVipSenderAsync(string email, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM VipSenders WHERE Email = @Email";
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task AddKeywordAsync(string keyword, string addedBy, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO AlertKeywords (Keyword, AddedBy, CreatedAt)
            VALUES (@Keyword, @AddedBy, @CreatedAt)
            """;
        cmd.Parameters.AddWithValue("@Keyword", keyword);
        cmd.Parameters.AddWithValue("@AddedBy", addedBy);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task RemoveKeywordAsync(string keyword, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM AlertKeywords WHERE Keyword = @Keyword";
        cmd.Parameters.AddWithValue("@Keyword", keyword);
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }
}
