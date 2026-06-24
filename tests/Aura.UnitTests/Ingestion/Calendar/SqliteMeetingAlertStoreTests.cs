using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Aura.Infrastructure.Adapters.Calendar;
using Microsoft.Data.Sqlite;

namespace Aura.UnitTests.Ingestion.Calendar;

public class SqliteMeetingAlertStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteMeetingAlertStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SqliteMeetingAlertStore.InitializeSchema(_connection);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task GetUnsentAlertAsync_WhenNoAlertExists_ReturnsNull()
    {
        var store = new SqliteMeetingAlertStore(_connection);

        var result = await store.GetUnsentAlertAsync("evt-1", MeetingAlertTrigger.SixtyMinutes, DateTimeOffset.UtcNow.Date, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task MarkSentAsync_ThenGetUnsent_ReturnsNull()
    {
        var store = new SqliteMeetingAlertStore(_connection);
        var alert = new MeetingAlert("evt-1", "Standup", MeetingAlertTrigger.SixtyMinutes, DateTimeOffset.UtcNow, null, "user-1");

        await store.MarkSentAsync(alert, CancellationToken.None);

        var result = await store.GetUnsentAlertAsync("evt-1", MeetingAlertTrigger.SixtyMinutes, DateTimeOffset.UtcNow.Date, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetUpcomingAlertsAsync_ReturnsAlertsInWindow()
    {
        var store = new SqliteMeetingAlertStore(_connection);
        var now = DateTimeOffset.UtcNow;
        var alert = new MeetingAlert("evt-1", "Review", MeetingAlertTrigger.TenMinutes, now.AddMinutes(10), null, "user-1");

        await store.MarkSentAsync(alert, CancellationToken.None);

        var results = await store.GetUpcomingAlertsAsync(now, now.AddMinutes(30), CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("evt-1", results[0].EventId);
        Assert.Equal(MeetingAlertTrigger.TenMinutes, results[0].Trigger);
    }
}
