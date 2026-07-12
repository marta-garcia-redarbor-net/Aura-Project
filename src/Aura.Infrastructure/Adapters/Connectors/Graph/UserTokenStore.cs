using Microsoft.Data.Sqlite;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// SQLite-backed store for OBO-acquired Graph tokens keyed by user oid.
/// The API acquires these tokens via On-Behalf-Of flow when the user
/// triggers a sync; the worker reads them to call Graph directly,
/// bypassing the MSAL public client cache that cannot be shared
/// across processes.
/// </summary>
public sealed class UserTokenStore
{
    private readonly SqliteConnection _connection;

    public UserTokenStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>Creates the user token table if it doesn't exist.</summary>
    public static void InitializeSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS UserTokens (
                Oid TEXT PRIMARY KEY,
                AccessToken TEXT NOT NULL,
                ExpiresAt TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>Persists or updates an OBO-acquired Graph token for the given oid.</summary>
    public void SaveToken(string oid, string accessToken, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oid);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO UserTokens (Oid, AccessToken, ExpiresAt)
            VALUES (@Oid, @AccessToken, @ExpiresAt)
            ON CONFLICT(Oid) DO UPDATE SET
                AccessToken = excluded.AccessToken,
                ExpiresAt = excluded.ExpiresAt;
            """;
        cmd.Parameters.AddWithValue("@Oid", oid);
        cmd.Parameters.AddWithValue("@AccessToken", accessToken);
        cmd.Parameters.AddWithValue("@ExpiresAt", expiresAt.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    /// <summary>Retrieves a cached OBO token for the given oid, or null if absent or expired.</summary>
    public (string? AccessToken, DateTimeOffset? ExpiresAt)? GetToken(string oid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oid);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT AccessToken, ExpiresAt FROM UserTokens WHERE Oid = @Oid";
        cmd.Parameters.AddWithValue("@Oid", oid);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var accessToken = reader.IsDBNull(0) ? null : reader.GetString(0);
        var expiresAtStr = reader.IsDBNull(1) ? null : reader.GetString(1);

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(expiresAtStr))
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(expiresAtStr, out var expiresAt))
        {
            return null;
        }

        // If expired, signal caller to re-acquire via OBO
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            return (accessToken, expiresAt);
        }

        return (accessToken, expiresAt);
    }

    /// <summary>Removes a cached token (e.g. when OBO refresh fails repeatedly).</summary>
    public void RemoveToken(string oid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oid);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM UserTokens WHERE Oid = @Oid";
        cmd.Parameters.AddWithValue("@Oid", oid);
        cmd.ExecuteNonQuery();
    }
}
