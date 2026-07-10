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

    // ---- Phase 2: GetPendingExternalIdsAsync ----

    [Fact]
    public async Task GetPendingExternalIdsAsync_ReturnsOnlyPending()
    {
        var pending = CreateWorkItem("ext-pend", "Pending Item");
        // Completed item
        var completed = CreateWorkItem("ext-comp", "Completed Item");
        completed.MarkProcessing();
        completed.MarkCompleted();

        await _store.SaveAsync(pending, CancellationToken.None);
        await _store.SaveAsync(completed, CancellationToken.None);

        var result = await _store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);

        Assert.Contains("ext-pend", result);
        Assert.DoesNotContain("ext-comp", result);
    }

    [Fact]
    public async Task GetPendingExternalIdsAsync_FiltersBySourceType()
    {
        var outlook = new WorkItem("ext-ol", "Outlook Item", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium, new Dictionary<string, string>());
        var teams = new WorkItem("ext-teams", "Teams Item", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium, new Dictionary<string, string>());

        await _store.SaveAsync(outlook, CancellationToken.None);
        await _store.SaveAsync(teams, CancellationToken.None);

        var result = await _store.GetPendingExternalIdsAsync(WorkItemSourceType.OutlookEmail, CancellationToken.None);

        Assert.Contains("ext-ol", result);
        Assert.DoesNotContain("ext-teams", result);
    }

    [Fact]
    public async Task GetPendingExternalIdsAsync_NoPending_ReturnsEmptySet()
    {
        var result = await _store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);

        Assert.Empty(result);
    }

    // ---- Phase 2: MarkCompletedAsync ----

    [Fact]
    public async Task MarkCompletedAsync_ChangesMultipleItems()
    {
        var itemA = CreateWorkItem("ext-a", "Item A");
        var itemB = CreateWorkItem("ext-b", "Item B");
        var itemC = CreateWorkItem("ext-c", "Item C");

        await _store.SaveAsync(itemA, CancellationToken.None);
        await _store.SaveAsync(itemB, CancellationToken.None);
        await _store.SaveAsync(itemC, CancellationToken.None);

        await _store.MarkCompletedAsync(new HashSet<string> { "ext-a", "ext-c" }, WorkItemSourceType.TeamsMessage, CancellationToken.None);

        var pending = await _store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);
        Assert.DoesNotContain("ext-a", pending);
        Assert.Contains("ext-b", pending);
        Assert.DoesNotContain("ext-c", pending);
    }

    [Fact]
    public async Task MarkCompletedAsync_IgnoresNonexistentIds()
    {
        var itemA = CreateWorkItem("ext-a", "Item A");
        await _store.SaveAsync(itemA, CancellationToken.None);

        // Should not throw when marking non-existent IDs
        var ex = await Record.ExceptionAsync(() =>
            _store.MarkCompletedAsync(new HashSet<string> { "ext-a", "nonexistent" }, WorkItemSourceType.TeamsMessage, CancellationToken.None));

        Assert.Null(ex);

        var pending = await _store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);
        Assert.DoesNotContain("ext-a", pending);
    }

    [Fact]
    public async Task MarkCompletedAsync_IncludesSourceFilter()
    {
        var outlook = new WorkItem("ext-ol", "Outlook Item", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium, new Dictionary<string, string>());
        var teams = new WorkItem("ext-teams", "Teams Item", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium, new Dictionary<string, string>());

        await _store.SaveAsync(outlook, CancellationToken.None);
        await _store.SaveAsync(teams, CancellationToken.None);

        // Mark outlook items as completed — Teams should remain Pending
        await _store.MarkCompletedAsync(new HashSet<string> { "ext-ol" }, WorkItemSourceType.OutlookEmail, CancellationToken.None);

        var teamsPending = await _store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);
        Assert.Contains("ext-teams", teamsPending);
    }

    // ---- Phase 4: Integration - SQLite round-trip ----

    [Fact]
    public async Task SqliteRoundTrip_InsertGetPendingMarkCompleted_VerifyState()
    {
        var outlook1 = new WorkItem("int-ol-1", "Outlook 1", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.High, new Dictionary<string, string>());
        var outlook2 = new WorkItem("int-ol-2", "Outlook 2", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium, new Dictionary<string, string>());
        var outlook3 = new WorkItem("int-ol-3", "Outlook 3", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Low, new Dictionary<string, string>());

        // Save all 3
        await _store.SaveAsync(outlook1, CancellationToken.None);
        await _store.SaveAsync(outlook2, CancellationToken.None);
        await _store.SaveAsync(outlook3, CancellationToken.None);

        // Verify all 3 are pending
        var pendingBefore = await _store.GetPendingExternalIdsAsync(WorkItemSourceType.OutlookEmail, CancellationToken.None);
        Assert.Equal(3, pendingBefore.Count);

        // Mark 2 as completed
        await _store.MarkCompletedAsync(new HashSet<string> { "int-ol-1", "int-ol-3" }, WorkItemSourceType.OutlookEmail, CancellationToken.None);

        // Verify only outlook2 remains pending
        var pendingAfter = await _store.GetPendingExternalIdsAsync(WorkItemSourceType.OutlookEmail, CancellationToken.None);
        Assert.Single(pendingAfter);
        Assert.Contains("int-ol-2", pendingAfter);

        // Verify via raw SQL that statuses changed
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT ExternalId, Status, UpdatedAt FROM WorkItems WHERE ExternalId IN ('int-ol-1', 'int-ol-2', 'int-ol-3') ORDER BY ExternalId";
        using var reader = cmd.ExecuteReader();

        Assert.True(reader.Read());
        Assert.Equal("int-ol-1", reader.GetString(0));
        Assert.Equal("Completed", reader.GetString(1));
        Assert.False(reader.IsDBNull(2), "UpdatedAt should be set for completed items");

        Assert.True(reader.Read());
        Assert.Equal("int-ol-2", reader.GetString(0));
        Assert.Equal("Pending", reader.GetString(1));
        Assert.True(reader.IsDBNull(2), "UpdatedAt should remain null for pending items");

        Assert.True(reader.Read());
        Assert.Equal("int-ol-3", reader.GetString(0));
        Assert.Equal("Completed", reader.GetString(1));
        Assert.False(reader.IsDBNull(2), "UpdatedAt should be set for completed items");
    }

    [Fact]
    public async Task SqliteIsolation_TeamsItemsNotAffectedByOutlookDiff()
    {
        // Insert Outlook items
        var outlook1 = new WorkItem("ol-iso-1", "Outlook 1", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium, new Dictionary<string, string>());
        var outlook2 = new WorkItem("ol-iso-2", "Outlook 2", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium, new Dictionary<string, string>());

        // Insert Teams items
        var teams1 = new WorkItem("teams-iso-1", "Teams 1", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium, new Dictionary<string, string>());
        var teams2 = new WorkItem("teams-iso-2", "Teams 2", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium, new Dictionary<string, string>());

        await _store.SaveAsync(outlook1, CancellationToken.None);
        await _store.SaveAsync(outlook2, CancellationToken.None);
        await _store.SaveAsync(teams1, CancellationToken.None);
        await _store.SaveAsync(teams2, CancellationToken.None);

        // Mark Outlook items as completed (as if diff ran)
        await _store.MarkCompletedAsync(
            new HashSet<string> { "ol-iso-1", "ol-iso-2" },
            WorkItemSourceType.OutlookEmail,
            CancellationToken.None);

        // Teams items should still be Pending
        var teamsPending = await _store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);
        Assert.Equal(2, teamsPending.Count);
        Assert.Contains("teams-iso-1", teamsPending);
        Assert.Contains("teams-iso-2", teamsPending);

        // Outlook should have no pending items
        var outlookPending = await _store.GetPendingExternalIdsAsync(WorkItemSourceType.OutlookEmail, CancellationToken.None);
        Assert.Empty(outlookPending);
    }

    // ---- W3-H3: PriorityScore persistence ----

    [Fact]
    public async Task SaveAsync_WithPriorityScore_PersistsAndReadsBack()
    {
        var item = new WorkItem("ext-ps-1", "Scored Item", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Critical,
            new Dictionary<string, string>(), priorityScore: 95);

        await _store.SaveAsync(item, CancellationToken.None);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT PriorityScore FROM WorkItems WHERE ExternalId = @ExternalId";
        cmd.Parameters.AddWithValue("@ExternalId", "ext-ps-1");
        var score = cmd.ExecuteScalar();

        Assert.Equal(95, Convert.ToInt32(score));
    }

    [Fact]
    public async Task SaveAsync_WithNullPriorityScore_PersistsNull()
    {
        var item = new WorkItem("ext-ps-null", "Null Score Item", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium,
            new Dictionary<string, string>(), priorityScore: null);

        await _store.SaveAsync(item, CancellationToken.None);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT PriorityScore FROM WorkItems WHERE ExternalId = @ExternalId";
        cmd.Parameters.AddWithValue("@ExternalId", "ext-ps-null");
        var score = cmd.ExecuteScalar();

        Assert.Equal(DBNull.Value, score);
    }

    [Fact]
    public async Task SaveAsync_WithPriorityScore_UpsertRetainsScore()
    {
        var item1 = new WorkItem("ext-ps-upd", "First", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.High,
            new Dictionary<string, string>(), priorityScore: 80);
        var item2 = new WorkItem("ext-ps-upd", "Second", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.High,
            new Dictionary<string, string>(), priorityScore: 90);

        await _store.SaveAsync(item1, CancellationToken.None);
        await _store.SaveAsync(item2, CancellationToken.None);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT PriorityScore, Title FROM WorkItems WHERE ExternalId = @ExternalId";
        cmd.Parameters.AddWithValue("@ExternalId", "ext-ps-upd");
        using var reader = cmd.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(90, reader.GetInt32(0));
        Assert.Equal("Second", reader.GetString(1));
    }

    [Fact]
    public async Task FindByExternalIdAsync_ReturnsPriorityScore()
    {
        var item = new WorkItem("ext-ps-find", "Findable Score", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Critical,
            new Dictionary<string, string>(), priorityScore: 88);

        await _store.SaveAsync(item, CancellationToken.None);
        var found = await _store.FindByExternalIdAsync("ext-ps-find", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(88, found!.PriorityScore);
    }

    [Fact]
    public async Task FindByExternalIdAsync_WhenNoScore_ReturnsNull()
    {
        var item = CreateWorkItem("ext-ps-noscore", "No Score");
        await _store.SaveAsync(item, CancellationToken.None);
        var found = await _store.FindByExternalIdAsync("ext-ps-noscore", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Null(found!.PriorityScore);
    }

    [Fact]
    public async Task ReadBySourceAsync_SortsByPriorityScoreDescWithCoalesce()
    {
        // Critical default = 100, High default = 75, Medium default = 50
        var critical = new WorkItem("ext-crit", "Critical", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Critical,
            new Dictionary<string, string>(), priorityScore: null);
        var high = new WorkItem("ext-high", "High", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.High,
            new Dictionary<string, string>(), priorityScore: null);
        var medium = new WorkItem("ext-med", "Medium", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium,
            new Dictionary<string, string>(), priorityScore: null);

        await _store.SaveAsync(medium, CancellationToken.None);
        await _store.SaveAsync(critical, CancellationToken.None);
        await _store.SaveAsync(high, CancellationToken.None);

        var results = await _store.ReadBySourceAsync(
            WorkItemSourceType.TeamsMessage, null, null, CancellationToken.None);

        // Critical (100) > High (75) > Medium (50)
        Assert.Equal("Critical", results[0].Priority.ToString());
        Assert.Equal("High", results[1].Priority.ToString());
        Assert.Equal("Medium", results[2].Priority.ToString());
    }

    [Fact]
    public async Task ReadBySourceAsync_ExplicitScoreWinsOverDefault()
    {
        // Medium with explicit 90 should sort before Critical with default 100
        // Actually, 90 < 100, so Critical still comes first. Let me fix this.
        // Medium with 90, High with null (default 75), Critical null (100)
        var mediumHighScore = new WorkItem("ext-med-hi", "Medium High", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium,
            new Dictionary<string, string>(), priorityScore: 90);
        var critical = new WorkItem("ext-crit", "Critical", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Critical,
            new Dictionary<string, string>(), priorityScore: null);
        var high = new WorkItem("ext-high", "High", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.High,
            new Dictionary<string, string>(), priorityScore: null);

        await _store.SaveAsync(high, CancellationToken.None);
        await _store.SaveAsync(mediumHighScore, CancellationToken.None);
        await _store.SaveAsync(critical, CancellationToken.None);

        var results = await _store.ReadBySourceAsync(
            WorkItemSourceType.TeamsMessage, null, null, CancellationToken.None);

        // COALESCE: Critical (100) > Medium(90) > High(75)
        Assert.Equal("Critical", results[0].Priority.ToString());
        Assert.Equal("Medium", results[1].Priority.ToString());
        Assert.Equal("High", results[2].Priority.ToString());
    }

    [Fact]
    public async Task ReadBySourceAsync_DefaultDerivationOrdersItems_WithoutMutatingNullPriorityScore()
    {
        var criticalNull = new WorkItem("ext-derive-crit", "Critical Null", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Critical,
            new Dictionary<string, string>(), priorityScore: null);
        var lowNull = new WorkItem("ext-derive-low", "Low Null", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Low,
            new Dictionary<string, string>(), priorityScore: null);

        await _store.SaveAsync(lowNull, CancellationToken.None);
        await _store.SaveAsync(criticalNull, CancellationToken.None);

        var results = await _store.ReadBySourceAsync(
            WorkItemSourceType.TeamsMessage, null, null, CancellationToken.None);

        Assert.Equal("ext-derive-crit", results[0].ExternalId);
        Assert.Equal("ext-derive-low", results[1].ExternalId);
        Assert.Null(results[0].PriorityScore);
        Assert.Null(results[1].PriorityScore);
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
