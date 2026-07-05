using Aura.Application.Ports;
using Aura.Domain.FocusState;
using Microsoft.Data.Sqlite;

namespace Aura.Infrastructure.Adapters.FocusState;

/// <summary>
/// SQLite-backed store for user-defined focus state overrides.
/// Uses the <c>FocusStateOverrides</c> table.
/// </summary>
internal sealed class SqliteFocusStateOverrideStore : IFocusStateOverrideStore
{
    private readonly SqliteConnection _connection;

    public SqliteFocusStateOverrideStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public static void InitializeSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS FocusStateOverrides (
                UserId TEXT PRIMARY KEY,
                State TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public Task<FocusStateType?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT State FROM FocusStateOverrides WHERE UserId = @UserId
            """;
        cmd.Parameters.AddWithValue("@UserId", userId);

        var result = cmd.ExecuteScalar();
        if (result is null || result == DBNull.Value)
            return Task.FromResult<FocusStateType?>(null);

        var state = Enum.Parse<FocusStateType>((string)result);
        return Task.FromResult<FocusStateType?>(state);
    }

    public Task SetAsync(string userId, FocusStateType state)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO FocusStateOverrides (UserId, State, CreatedAt, UpdatedAt)
            VALUES (@UserId, @State, @CreatedAt, NULL)
            ON CONFLICT(UserId) DO UPDATE SET
                State = excluded.State,
                UpdatedAt = @UpdatedAt
            """;
        var now = DateTimeOffset.UtcNow.ToString("O");
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@State", state.ToString());
        cmd.Parameters.AddWithValue("@CreatedAt", now);
        cmd.Parameters.AddWithValue("@UpdatedAt", now);
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task ClearAsync(string userId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            DELETE FROM FocusStateOverrides WHERE UserId = @UserId
            """;
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }
}
