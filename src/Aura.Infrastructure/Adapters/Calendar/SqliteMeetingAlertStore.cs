using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Microsoft.Data.Sqlite;

namespace Aura.Infrastructure.Adapters.Calendar;

internal sealed class SqliteMeetingAlertStore : IMeetingAlertStore
{
    private readonly SqliteConnection _connection;

    public SqliteMeetingAlertStore(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public static void InitializeSchema(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS MeetingAlerts (
                EventId TEXT NOT NULL,
                Trigger TEXT NOT NULL,
                LocalDate TEXT NOT NULL,
                Title TEXT NOT NULL,
                StartsAtUtc TEXT NOT NULL,
                JoinUrl TEXT NULL,
                UserId TEXT NOT NULL,
                HasBeenSent INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (EventId, Trigger, LocalDate)
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public Task<MeetingAlert?> GetUnsentAlertAsync(string eventId, MeetingAlertTrigger trigger, DateTimeOffset date, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT Title, StartsAtUtc, JoinUrl, UserId, HasBeenSent
            FROM MeetingAlerts
            WHERE EventId = @EventId AND Trigger = @Trigger AND LocalDate = @LocalDate AND HasBeenSent = 0
            """;
        cmd.Parameters.AddWithValue("@EventId", eventId);
        cmd.Parameters.AddWithValue("@Trigger", trigger.ToString());
        cmd.Parameters.AddWithValue("@LocalDate", date.ToString("yyyy-MM-dd"));

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<MeetingAlert?>(null);
        }

        var alert = new MeetingAlert(
            eventId,
            reader.GetString(0),
            trigger,
            DateTimeOffset.Parse(reader.GetString(1)),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4) == 1);

        return Task.FromResult<MeetingAlert?>(alert);
    }

    public Task MarkSentAsync(MeetingAlert alert, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(alert);
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO MeetingAlerts (EventId, Trigger, LocalDate, Title, StartsAtUtc, JoinUrl, UserId, HasBeenSent)
            VALUES (@EventId, @Trigger, @LocalDate, @Title, @StartsAtUtc, @JoinUrl, @UserId, 1)
            ON CONFLICT(EventId, Trigger, LocalDate) DO UPDATE SET
                HasBeenSent = 1;
            """;
        cmd.Parameters.AddWithValue("@EventId", alert.EventId);
        cmd.Parameters.AddWithValue("@Trigger", alert.Trigger.ToString());
        cmd.Parameters.AddWithValue("@LocalDate", alert.StartsAtUtc.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@Title", alert.Title);
        cmd.Parameters.AddWithValue("@StartsAtUtc", alert.StartsAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@JoinUrl", (object?)alert.JoinUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@UserId", alert.UserId);
        cmd.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MeetingAlert>> GetUpcomingAlertsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT EventId, Title, Trigger, StartsAtUtc, JoinUrl, UserId, HasBeenSent
            FROM MeetingAlerts
            WHERE StartsAtUtc >= @FromUtc AND StartsAtUtc <= @ToUtc
            ORDER BY StartsAtUtc ASC
            """;
        cmd.Parameters.AddWithValue("@FromUtc", from.ToString("O"));
        cmd.Parameters.AddWithValue("@ToUtc", to.ToString("O"));

        var alerts = new List<MeetingAlert>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var trigger = Enum.Parse<MeetingAlertTrigger>(reader.GetString(2));
            alerts.Add(new MeetingAlert(
                reader.GetString(0),
                reader.GetString(1),
                trigger,
                DateTimeOffset.Parse(reader.GetString(3)),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetString(5),
                reader.GetInt32(6) == 1));
        }

        return Task.FromResult<IReadOnlyList<MeetingAlert>>(alerts);
    }
}
