using Aura.Application.Ports;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Aura.Infrastructure.Adapters.MorningSummaryScheduling;

public sealed class SqliteMorningSummaryEmissionStore : IMorningSummaryEmissionStore
{
    private readonly SqliteConnection _connection;

    public SqliteMorningSummaryEmissionStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public static void InitializeSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS MorningSummaryEmission (
                UserId TEXT NOT NULL,
                LocalDate TEXT NOT NULL,
                EmittedAt TEXT NOT NULL,
                PRIMARY KEY (UserId, LocalDate)
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public Task<bool> HasBeenEmittedAsync(string userId, DateOnly localDate, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT 1
            FROM MorningSummaryEmission
            WHERE UserId = @UserId AND LocalDate = @LocalDate
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@LocalDate", FormatLocalDate(localDate));

        var result = cmd.ExecuteScalar();
        return Task.FromResult(result is not null);
    }

    public Task MarkEmittedAsync(string userId, DateOnly localDate, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO MorningSummaryEmission (UserId, LocalDate, EmittedAt)
            VALUES (@UserId, @LocalDate, @EmittedAt);
            """;
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@LocalDate", FormatLocalDate(localDate));
        cmd.Parameters.AddWithValue("@EmittedAt", DateTimeOffset.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task ResetAsync(string userId, DateOnly localDate, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            DELETE FROM MorningSummaryEmission
            WHERE UserId = @UserId AND LocalDate = @LocalDate;
            """;
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@LocalDate", FormatLocalDate(localDate));
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private static string FormatLocalDate(DateOnly localDate)
        => localDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
