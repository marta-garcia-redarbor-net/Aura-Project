using Microsoft.Data.Sqlite;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// SQLite-backed MSAL token cache. Stores serialized MSAL cache blobs keyed by cache key.
/// Can be hooked into MSAL's ITokenCache via SetBeforeAccessAsync/SetAfterAccessAsync.
/// </summary>
internal sealed class MsalSqliteTokenCache
{
    private readonly SqliteConnection _connection;

    public MsalSqliteTokenCache(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <summary>Creates the token cache table if it doesn't exist.</summary>
    public static void InitializeSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS MsalTokenCache (
                CacheKey TEXT PRIMARY KEY,
                Data BLOB NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS MsalUserIndex (
                Oid TEXT PRIMARY KEY,
                AccountId TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>Persists the MSAL cache blob for the given key (upsert).</summary>
    public void Persist(string cacheKey, byte[] data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        ArgumentNullException.ThrowIfNull(data);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO MsalTokenCache (CacheKey, Data, UpdatedAt)
            VALUES (@CacheKey, @Data, @UpdatedAt)
            ON CONFLICT(CacheKey) DO UPDATE SET
                Data = excluded.Data,
                UpdatedAt = excluded.UpdatedAt;
            """;
        cmd.Parameters.AddWithValue("@CacheKey", cacheKey);
        cmd.Parameters.AddWithValue("@Data", data);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    /// <summary>Retrieves the cached MSAL blob for the given key, or null if absent.</summary>
    public byte[]? Retrieve(string cacheKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Data FROM MsalTokenCache WHERE CacheKey = @CacheKey";
        cmd.Parameters.AddWithValue("@CacheKey", cacheKey);

        var result = cmd.ExecuteScalar();
        return result is byte[] blob ? blob : null;
    }

    /// <summary>Checks if there is cached data for the given key.</summary>
    public bool HasCachedData(string cacheKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM MsalTokenCache WHERE CacheKey = @CacheKey";
        cmd.Parameters.AddWithValue("@CacheKey", cacheKey);

        return Convert.ToInt64(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>Persists oid -> MSAL account identifier mapping for partitioned account resolution.</summary>
    public void PersistUserAccount(string oid, string accountId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oid);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO MsalUserIndex (Oid, AccountId, UpdatedAt)
            VALUES (@Oid, @AccountId, @UpdatedAt)
            ON CONFLICT(Oid) DO UPDATE SET
                AccountId = excluded.AccountId,
                UpdatedAt = excluded.UpdatedAt;
            """;
        cmd.Parameters.AddWithValue("@Oid", oid);
        cmd.Parameters.AddWithValue("@AccountId", accountId);
        cmd.Parameters.AddWithValue("@UpdatedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    /// <summary>Resolves the cached MSAL account identifier for an oid, if present.</summary>
    public string? GetAccountIdByOid(string oid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oid);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT AccountId FROM MsalUserIndex WHERE Oid = @Oid";
        cmd.Parameters.AddWithValue("@Oid", oid);

        var result = cmd.ExecuteScalar();
        return result as string;
    }

    /// <summary>Lists all cached user oids in the token-cache user index.</summary>
    public IReadOnlyList<string> ListUserOids()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Oid FROM MsalUserIndex ORDER BY Oid";

        using var reader = cmd.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
        {
            if (!reader.IsDBNull(0))
            {
                var oid = reader.GetString(0);
                if (!string.IsNullOrWhiteSpace(oid))
                {
                    result.Add(oid);
                }
            }
        }

        return result;
    }
}
