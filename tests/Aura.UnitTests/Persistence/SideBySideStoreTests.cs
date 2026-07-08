using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Aura.Domain.FocusState;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Calendar;
using Aura.Infrastructure.Adapters.FocusState;
using Aura.Infrastructure.Adapters.MorningSummaryScheduling;
using Aura.Infrastructure.Adapters.WorkItems;
using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Aura.UnitTests.Persistence;

/// <summary>
/// Side-by-side integration tests that seed data via the legacy SQLite stores
/// and read the same data via the new EF Core stores, asserting identical results.
/// Both store families share the same in-memory SQLite database to prove schema
/// compatibility and data fidelity across the migration boundary.
/// </summary>
public class SideBySideStoreTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AuraDbContext _db;

    public SideBySideStoreTests()
    {
        _connection = new SqliteConnection($"Data Source=side-by-side-{Guid.NewGuid()};Mode=Memory;Cache=Shared");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AuraDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task WorkItem_SeedViaSqlite_ReadViaEfCore_ReturnsIdenticalData()
    {
        // Arrange: seed via SQLite store
        var sqliteStore = new SqliteWorkItemStore(_connection);
        SqliteWorkItemStore.InitializeSchema(_connection);

        var item = new WorkItem(
            externalId: "sbs-ext-1",
            title: "Side-by-side test item",
            source: "teams",
            sourceType: WorkItemSourceType.TeamsMessage,
            priority: WorkItemPriority.High,
            metadata: new Dictionary<string, string> { ["channel"] = "general" });

        await sqliteStore.SaveAsync(item, CancellationToken.None);

        // Act: read via EF Core store
        var efStore = new EfWorkItemStore(_db);
        var found = await efStore.FindByExternalIdAsync("sbs-ext-1", CancellationToken.None);

        // Assert: identical field values
        Assert.NotNull(found);
        Assert.Equal("Side-by-side test item", found.Title);
        Assert.Equal("teams", found.Source);
        Assert.Equal(WorkItemSourceType.TeamsMessage, found.SourceType);
        Assert.Equal(WorkItemPriority.High, found.Priority);
        Assert.Equal("general", found.Metadata["channel"]);
    }

    [Fact]
    public async Task WorkItem_SeedMultipleViaSqlite_ReadViaEfCore_ReturnsSameCount()
    {
        // Arrange: seed 3 items via SQLite store
        var sqliteStore = new SqliteWorkItemStore(_connection);
        SqliteWorkItemStore.InitializeSchema(_connection);

        for (var i = 0; i < 3; i++)
        {
            var item = new WorkItem(
                externalId: $"sbs-multi-{i}",
                title: $"Item {i}",
                source: "outlook",
                sourceType: WorkItemSourceType.OutlookEmail,
                priority: WorkItemPriority.Medium,
                metadata: new Dictionary<string, string>());
            await sqliteStore.SaveAsync(item, CancellationToken.None);
        }

        // Act: read all pending via EF Core store
        var efStore = new EfWorkItemStore(_db);
        var pendingIds = await efStore.GetPendingExternalIdsAsync(WorkItemSourceType.OutlookEmail, CancellationToken.None);

        // Assert: same count
        Assert.Equal(3, pendingIds.Count);
        Assert.Contains("sbs-multi-0", pendingIds);
        Assert.Contains("sbs-multi-1", pendingIds);
        Assert.Contains("sbs-multi-2", pendingIds);
    }

    [Fact]
    public async Task FocusState_SeedViaSqlite_ReadViaEfCore_ReturnsIdenticalState()
    {
        // Arrange: seed via SQLite store
        var sqliteStore = new SqliteFocusStateOverrideStore(_connection);
        SqliteFocusStateOverrideStore.InitializeSchema(_connection);

        await sqliteStore.SetAsync("sbs-user-1", FocusStateType.DeepWork);

        // Act: read via EF Core store
        var efStore = new EfFocusStateOverrideStore(_db);
        var result = await efStore.GetAsync("sbs-user-1");

        // Assert: identical state
        Assert.Equal(FocusStateType.DeepWork, result);
    }

    [Fact]
    public async Task FocusState_SeedViaSqlite_ClearViaEfCore_ReadViaSqlite_ReturnsNull()
    {
        // Arrange: seed via SQLite store
        var sqliteStore = new SqliteFocusStateOverrideStore(_connection);
        SqliteFocusStateOverrideStore.InitializeSchema(_connection);
        await sqliteStore.SetAsync("sbs-user-2", FocusStateType.Away);

        // Act: clear via EF Core store, read via SQLite store
        var efStore = new EfFocusStateOverrideStore(_db);
        await efStore.ClearAsync("sbs-user-2");
        var result = await sqliteStore.GetAsync("sbs-user-2");

        // Assert: cross-store clear is visible
        Assert.Null(result);
    }

    [Fact]
    public async Task MorningSummaryEmission_SeedViaSqlite_ReadViaEfCore_ReturnsIdenticalResult()
    {
        // Arrange: seed via SQLite store
        var sqliteStore = new SqliteMorningSummaryEmissionStore(_connection);
        SqliteMorningSummaryEmissionStore.InitializeSchema(_connection);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await sqliteStore.MarkEmittedAsync("sbs-user-1", today, CancellationToken.None);

        // Act: read via EF Core store
        var efStore = new EfMorningSummaryEmissionStore(_db);
        var result = await efStore.HasBeenEmittedAsync("sbs-user-1", today, CancellationToken.None);

        // Assert: identical result
        Assert.True(result);
    }

    [Fact]
    public async Task MorningSummaryEmission_SeedViaEfCore_ReadViaSqlite_ReturnsIdenticalResult()
    {
        // Arrange: seed via EF Core store
        var efStore = new EfMorningSummaryEmissionStore(_db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await efStore.MarkEmittedAsync("sbs-user-2", today, CancellationToken.None);

        // Act: read via SQLite store
        var sqliteStore = new SqliteMorningSummaryEmissionStore(_connection);
        SqliteMorningSummaryEmissionStore.InitializeSchema(_connection);
        var result = await sqliteStore.HasBeenEmittedAsync("sbs-user-2", today, CancellationToken.None);

        // Assert: identical result (reverse direction)
        Assert.True(result);
    }

    [Fact]
    public async Task MeetingAlert_SeedViaSqlite_ReadViaEfCore_ReturnsIdenticalAlerts()
    {
        // Arrange: seed via SQLite store
        var sqliteStore = new SqliteMeetingAlertStore(_connection);
        SqliteMeetingAlertStore.InitializeSchema(_connection);

        var now = DateTimeOffset.UtcNow;
        var alert = new MeetingAlert(
            "sbs-evt-1", "Standup", MeetingAlertTrigger.TenMinutes,
            now.AddMinutes(30), "https://meet.example.com", "sbs-user-1");

        await sqliteStore.MarkSentAsync(alert, CancellationToken.None);

        // Act: read via EF Core store
        var efStore = new EfMeetingAlertStore(_db);
        var upcoming = await efStore.GetUpcomingAlertsAsync(
            now.AddHours(-1), now.AddHours(1), CancellationToken.None);

        // Assert: identical data
        Assert.Single(upcoming);
        Assert.Equal("Standup", upcoming[0].Title);
        Assert.Equal("sbs-evt-1", upcoming[0].EventId);
        Assert.Equal(MeetingAlertTrigger.TenMinutes, upcoming[0].Trigger);
    }

    [Fact]
    public async Task MeetingAlert_SeedViaEfCore_ReadViaSqlite_ReturnsIdenticalAlerts()
    {
        // Arrange: seed via EF Core store
        var efStore = new EfMeetingAlertStore(_db);
        var now = DateTimeOffset.UtcNow;
        var alert = new MeetingAlert(
            "sbs-evt-2", "Review", MeetingAlertTrigger.FiveMinutes,
            now.AddMinutes(15), null, "sbs-user-2");

        await efStore.MarkSentAsync(alert, CancellationToken.None);

        // Act: read via SQLite store
        var sqliteStore = new SqliteMeetingAlertStore(_connection);
        SqliteMeetingAlertStore.InitializeSchema(_connection);
        var upcoming = await sqliteStore.GetUpcomingAlertsAsync(
            now.AddHours(-1), now.AddHours(1), CancellationToken.None);

        // Assert: identical data (reverse direction)
        Assert.Single(upcoming);
        Assert.Equal("Review", upcoming[0].Title);
        Assert.Equal("sbs-evt-2", upcoming[0].EventId);
    }
}
