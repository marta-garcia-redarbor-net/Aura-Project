using Aura.Application.Models;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.WorkItems;
using Microsoft.Data.Sqlite;

namespace Aura.IntegrationTests.WorkItems;

/// <summary>
/// Integration test proving SqliteWorkItemStore round-trips chat-level WorkItems
/// through save, FindByExternalId, and ReadForWindow with status filter.
/// </summary>
public sealed class WorkItemStoreRoundTripTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteWorkItemStore _store;

    public WorkItemStoreRoundTripTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        SqliteWorkItemStore.InitializeSchema(_connection);
        _store = new SqliteWorkItemStore(_connection);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task SaveChatWorkItem_FindByExternalId_ReturnsChatWorkItem()
    {
        var metadata = new Dictionary<string, string>
        {
            ["chats.lastMessageAt"] = "2026-06-30T15:00:00Z",
            ["chats.lastMessageReadAt"] = "2026-06-30T14:30:00Z",
            ["chats.unreadCount"] = "5"
        };
        var item = new WorkItem(
            "19:abc@thread.v2",
            "Chat about sprint review",
            "chats",
            WorkItemSourceType.TeamsChat,
            WorkItemPriority.High,
            metadata);

        await _store.SaveAsync(item, CancellationToken.None);

        var found = await _store.FindByExternalIdAsync("19:abc@thread.v2", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("19:abc@thread.v2", found!.ExternalId);
        Assert.Equal("chats", found.Source);
        Assert.Equal(WorkItemSourceType.TeamsChat, found.SourceType);
        Assert.Equal("Chat about sprint review", found.Title);
        Assert.Equal(WorkItemPriority.High, found.Priority);
        Assert.Equal("5", found.Metadata["chats.unreadCount"]);
    }

    [Fact]
    public async Task FindByExternalId_NonExistentId_ReturnsNull()
    {
        var found = await _store.FindByExternalIdAsync("nonexistent-chat-id", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task ReadForWindowWithPendingFilter_IncludesChatWorkItem()
    {
        var chatMeta = new Dictionary<string, string>
        {
            ["chats.lastMessageAt"] = "2026-06-30T15:00:00Z",
            ["chats.unreadCount"] = "3"
        };
        var chatItem = new WorkItem(
            "19:abc@thread.v2",
            "Chat about architecture",
            "chats",
            WorkItemSourceType.TeamsChat,
            WorkItemPriority.Medium,
            chatMeta);

        var msgItem = new WorkItem(
            "19:abc@thread.v2:msg123",
            "A message in channels",
            "messages",
            WorkItemSourceType.TeamsMessage,
            WorkItemPriority.Low,
            new Dictionary<string, string>());

        await _store.SaveAsync(chatItem, CancellationToken.None);
        await _store.SaveAsync(msgItem, CancellationToken.None);

        var query = new MorningSummaryQuery("user",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));
        var results = await _store.ReadForWindowAsync(query, WorkItemStatus.Pending, CancellationToken.None);

        Assert.Contains(results, item => item.ExternalId == "19:abc@thread.v2");
        Assert.Contains(results, item => item.ExternalId == "19:abc@thread.v2:msg123");
        Assert.All(results, item => Assert.Equal(WorkItemStatus.Pending, item.Status));
    }

    [Fact]
    public async Task ReadForWindowWithCompletedFilter_ExcludesPendingChatItem()
    {
        var chatItem = new WorkItem(
            "19:abc@thread.v2",
            "Chat about architecture",
            "chats",
            WorkItemSourceType.TeamsChat,
            WorkItemPriority.Medium,
            new Dictionary<string, string>());

        await _store.SaveAsync(chatItem, CancellationToken.None);

        var query = new MorningSummaryQuery("user",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));
        var results = await _store.ReadForWindowAsync(query, WorkItemStatus.Completed, CancellationToken.None);

        Assert.DoesNotContain(results, item => item.ExternalId == "19:abc@thread.v2");
    }
}
