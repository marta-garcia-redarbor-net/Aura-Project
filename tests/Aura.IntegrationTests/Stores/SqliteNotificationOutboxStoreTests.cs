using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Notifications;
using Microsoft.Data.Sqlite;

namespace Aura.IntegrationTests.Stores;

public sealed class SqliteNotificationOutboxStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteNotificationOutboxStore _store;

    public SqliteNotificationOutboxStoreTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SqliteNotificationOutboxStore.InitializeSchema(_connection);
        _store = new SqliteNotificationOutboxStore(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task EnqueueAndGetPending_ReturnsEnqueuedEntry()
    {
        var entry = new NotificationOutboxEntry(
            Guid.NewGuid(), "user-1", "OutlookEmail",
            "Urgent: Server Down", 9.5, "ScoreThresholdRule");

        await _store.EnqueueAsync(entry, CancellationToken.None);
        var pending = await _store.GetPendingAsync(10, CancellationToken.None);

        Assert.Single(pending);
        Assert.Equal(entry.Id, pending[0].Id);
        Assert.Equal("user-1", pending[0].UserId);
        Assert.Equal("Urgent: Server Down", pending[0].Title);
        Assert.Equal(9.5, pending[0].Priority);
        Assert.Equal("ScoreThresholdRule", pending[0].TriggerRule);
    }

    [Fact]
    public async Task MarkDispatched_RemovesFromPending()
    {
        var entry = new NotificationOutboxEntry(
            Guid.NewGuid(), "user-1", "OutlookEmail",
            "Test Item", 5.0);

        await _store.EnqueueAsync(entry, CancellationToken.None);
        await _store.MarkDispatchedAsync(entry.Id, CancellationToken.None);

        var pending = await _store.GetPendingAsync(10, CancellationToken.None);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task GetPending_OrdersByPriorityDescThenCreatedAtAsc()
    {
        var low = new NotificationOutboxEntry(
            Guid.NewGuid(), "user-1", "OutlookEmail", "Low Priority", 2.0);
        var high = new NotificationOutboxEntry(
            Guid.NewGuid(), "user-1", "OutlookEmail", "High Priority", 9.0);
        var medium = new NotificationOutboxEntry(
            Guid.NewGuid(), "user-1", "OutlookEmail", "Medium Priority", 5.0);

        await _store.EnqueueAsync(high, CancellationToken.None);
        await _store.EnqueueAsync(low, CancellationToken.None);
        await _store.EnqueueAsync(medium, CancellationToken.None);

        var pending = await _store.GetPendingAsync(10, CancellationToken.None);

        Assert.Equal(3, pending.Count);
        Assert.Equal("High Priority", pending[0].Title);
        Assert.Equal("Medium Priority", pending[1].Title);
        Assert.Equal("Low Priority", pending[2].Title);
    }
}
