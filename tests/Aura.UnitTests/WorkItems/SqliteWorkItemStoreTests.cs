using Aura.Application.Models;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.WorkItems;
using Microsoft.Data.Sqlite;

namespace Aura.UnitTests.WorkItems;

public class SqliteWorkItemStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteWorkItemStore _store;

    public SqliteWorkItemStoreTests()
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
    public async Task SaveAsync_NewWorkItem_ReturnsSuccess()
    {
        var item = CreateWorkItem("ext-1", "Test Title");

        var result = await _store.SaveAsync(item, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SaveAsync_DuplicateExternalId_Upserts_ReturnsSuccess()
    {
        var item1 = CreateWorkItem("ext-dup", "Original Title");
        var item2 = CreateWorkItem("ext-dup", "Updated Title");

        await _store.SaveAsync(item1, CancellationToken.None);
        var result = await _store.SaveAsync(item2, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SaveAsync_WorkItem_CanBeReadBack()
    {
        var item = CreateWorkItem("ext-read", "Readable Item");

        await _store.SaveAsync(item, CancellationToken.None);

        // Verify directly via SQL that the row was persisted
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT ExternalId, Title, Source, SourceType, Priority FROM WorkItems WHERE ExternalId = @ExternalId";
        cmd.Parameters.AddWithValue("@ExternalId", "ext-read");
        using var reader = cmd.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal("ext-read", reader.GetString(0));
        Assert.Equal("Readable Item", reader.GetString(1));
        Assert.Equal("messages", reader.GetString(2));
    }

    [Fact]
    public async Task SaveAsync_PreservesMetadata()
    {
        var metadata = new Dictionary<string, string>
        {
            ["teams.teamId"] = "team-a",
            ["teams.channelId"] = "channel-b"
        };
        var item = new WorkItem("ext-meta", "Meta Item", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.High, metadata);

        await _store.SaveAsync(item, CancellationToken.None);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT MetadataJson FROM WorkItems WHERE ExternalId = @ExternalId";
        cmd.Parameters.AddWithValue("@ExternalId", "ext-meta");
        var json = (string)cmd.ExecuteScalar()!;
        Assert.Contains("teams.teamId", json);
        Assert.Contains("team-a", json);
    }

    [Fact]
    public async Task SaveAsync_UpsertOverwritesTitle()
    {
        var item1 = CreateWorkItem("ext-upd", "Old Title");
        var item2 = CreateWorkItem("ext-upd", "New Title");

        await _store.SaveAsync(item1, CancellationToken.None);
        await _store.SaveAsync(item2, CancellationToken.None);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Title FROM WorkItems WHERE ExternalId = @ExternalId";
        cmd.Parameters.AddWithValue("@ExternalId", "ext-upd");
        var title = (string)cmd.ExecuteScalar()!;
        Assert.Equal("New Title", title);
    }

    [Fact]
    public async Task ReadForWindowAsync_WithPendingFilter_ReturnsOnlyPendingItems()
    {
        var item1 = CreateWorkItem("ext-pending", "Pending Item", WorkItemStatus.Pending);
        var item2 = CreateWorkItem("ext-completed", "Completed Item", WorkItemStatus.Completed);

        await _store.SaveAsync(item1, CancellationToken.None);
        await _store.SaveAsync(item2, CancellationToken.None);

        var query = new MorningSummaryQuery("user",
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var results = await _store.ReadForWindowAsync(query, WorkItemStatus.Pending, CancellationToken.None);

        Assert.Single(results);
        Assert.All(results, item => Assert.Equal(WorkItemStatus.Pending, item.Status));
    }

    [Fact]
    public async Task ReadForWindowAsync_WithFilterNoMatch_ReturnsEmpty()
    {
        var item = CreateWorkItem("ext-pending", "Pending Item", WorkItemStatus.Pending);
        await _store.SaveAsync(item, CancellationToken.None);

        var query = new MorningSummaryQuery("user",
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        var results = await _store.ReadForWindowAsync(query, WorkItemStatus.Processing, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SaveAsync_NullItem_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _store.SaveAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task FindByExternalIdAsync_ExistingId_ReturnsWorkItem()
    {
        var item = CreateWorkItem("ext-find", "Findable Item");
        await _store.SaveAsync(item, CancellationToken.None);

        var found = await _store.FindByExternalIdAsync("ext-find", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("ext-find", found!.ExternalId);
        Assert.Equal("Findable Item", found.Title);
    }

    [Fact]
    public async Task FindByExternalIdAsync_NonExistentId_ReturnsNull()
    {
        var found = await _store.FindByExternalIdAsync("nonexistent", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task SaveAsync_UpsertRetainsOriginalPriority()
    {
        var item1 = new WorkItem("ext-pri", "First Priority", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.High,
            new Dictionary<string, string>());
        var item2 = new WorkItem("ext-pri", "Second Priority", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Low,
            new Dictionary<string, string>());

        await _store.SaveAsync(item1, CancellationToken.None);
        await _store.SaveAsync(item2, CancellationToken.None);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Priority FROM WorkItems WHERE ExternalId = @ExternalId";
        cmd.Parameters.AddWithValue("@ExternalId", "ext-pri");
        var priority = (string)cmd.ExecuteScalar()!;
        // Priority from first save should be retained
        Assert.Equal("High", priority);
    }

    private static WorkItem CreateWorkItem(string externalId, string title) =>
        new(externalId, title, "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium,
            new Dictionary<string, string>());

    private static WorkItem CreateWorkItem(string externalId, string title, WorkItemStatus targetStatus)
    {
        var item = new WorkItem(externalId, title, "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium,
            new Dictionary<string, string>());

        if (targetStatus == WorkItemStatus.Processing)
        {
            item.MarkProcessing();
        }
        else if (targetStatus == WorkItemStatus.Completed)
        {
            item.MarkProcessing();
            item.MarkCompleted();
        }
        else if (targetStatus == WorkItemStatus.Faulted)
        {
            item.MarkProcessing();
            item.MarkFaulted("test fault");
        }

        return item;
    }
}
