using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Notifications;
using Microsoft.Data.Sqlite;

namespace Aura.UnitTests.Infrastructure;

public class SqliteNotificationOutboxStoreTests : IDisposable
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
        _connection?.Close();
        _connection?.Dispose();
    }

    [Fact]
    public async Task EnqueueAndGetPending_WithFullVerdict_RoundTripsCorrectly()
    {
        var entry = new NotificationOutboxEntry(
            workItemId: Guid.NewGuid(),
            userId: "user-abc",
            sourceType: "TeamsMessage",
            title: "Urgent message",
            priority: 5.0,
            triggerRule: "vip_sender",
            explanation: "VIP sender detected",
            decision: "InterruptNow",
            targetUserId: "user-abc",
            ruleResults: "[{\"ruleName\":\"vip_sender\",\"matched\":true,\"score\":9.0,\"confidence\":0.95}]");

        await _store.EnqueueAsync(entry, CancellationToken.None);
        var pending = await _store.GetPendingAsync(10, CancellationToken.None);

        Assert.Single(pending);
        var loaded = pending[0];
        Assert.Equal(entry.Id, loaded.Id);
        Assert.Equal(entry.WorkItemId, loaded.WorkItemId);
        Assert.Equal(entry.UserId, loaded.UserId);
        Assert.Equal(entry.SourceType, loaded.SourceType);
        Assert.Equal(entry.Title, loaded.Title);
        Assert.Equal(entry.Priority, loaded.Priority);
        Assert.Equal(entry.TriggerRule, loaded.TriggerRule);
        Assert.Equal(entry.Explanation, loaded.Explanation);
        Assert.Equal(entry.Decision, loaded.Decision);
        Assert.Equal(entry.TargetUserId, loaded.TargetUserId);
        Assert.Equal(entry.RuleResults, loaded.RuleResults);
    }

    [Fact]
    public async Task EnqueueAndGetPending_WithoutVerdict_AllFieldsAreNull()
    {
        var entry = new NotificationOutboxEntry(
            workItemId: Guid.NewGuid(),
            userId: "user-xyz",
            sourceType: "TeamsMessage",
            title: "Normal message",
            priority: 3.0,
            triggerRule: "some_rule");

        await _store.EnqueueAsync(entry, CancellationToken.None);
        var pending = await _store.GetPendingAsync(10, CancellationToken.None);

        Assert.Single(pending);
        var loaded = pending[0];
        Assert.Equal(entry.Id, loaded.Id);
        Assert.Equal(entry.TriggerRule, loaded.TriggerRule);
        Assert.Null(loaded.Explanation);
        Assert.Null(loaded.Decision);
        Assert.Null(loaded.TargetUserId);
        Assert.Null(loaded.RuleResults);
    }

    [Fact]
    public async Task MarkDispatchedAsync_MarksEntryAsDispatched()
    {
        var entry = new NotificationOutboxEntry(
            workItemId: Guid.NewGuid(),
            userId: "user-abc",
            sourceType: "TeamsMessage",
            title: "Test",
            priority: 1.0);

        await _store.EnqueueAsync(entry, CancellationToken.None);
        await _store.MarkDispatchedAsync(entry.Id, CancellationToken.None);

        var pending = await _store.GetPendingAsync(10, CancellationToken.None);
        Assert.Empty(pending);
    }
}
