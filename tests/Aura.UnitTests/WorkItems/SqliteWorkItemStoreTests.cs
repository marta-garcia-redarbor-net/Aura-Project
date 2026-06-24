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
    public async Task SaveAsync_NullItem_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _store.SaveAsync(null!, CancellationToken.None));
    }

    private static WorkItem CreateWorkItem(string externalId, string title) =>
        new(externalId, title, "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium,
            new Dictionary<string, string>());
}
