using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Aura.Domain.FocusState;
using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Calendar;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.Decisions;
using Aura.Infrastructure.Adapters.FocusState;
using Aura.Infrastructure.Adapters.MorningSummaryScheduling;
using Aura.Infrastructure.Adapters.Notifications;
using Aura.Infrastructure.Adapters.Rules;
using Aura.Infrastructure.Adapters.SemanticOutbox;
using Aura.Infrastructure.Adapters.WorkItems;
using Aura.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aura.UnitTests.Persistence;

/// <summary>
/// Tests for all 9 EF Core store implementations.
/// Each store is tested against an in-memory SQLite-backed AuraDbContext
/// to verify it fulfils the same port contract as the existing SQLite stores.
/// </summary>
public class EfStoreTests : IDisposable
{
    private readonly AuraDbContext _db;

    public EfStoreTests()
    {
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseSqlite($"Data Source=ef-store-tests-{Guid.NewGuid()};Mode=Memory;Cache=Shared")
            .Options;

        _db = new AuraDbContext(options);

        // Open the in-memory connection so it stays alive for the test lifetime
        var conn = _db.Database.GetDbConnection();
        conn.Open();
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    #region 2.1 EfFocusStateOverrideStore

    [Fact]
    public async Task FocusStateOverride_GetAsync_ReturnsNull_WhenNoOverrideExists()
    {
        var store = new EfFocusStateOverrideStore(_db);

        var result = await store.GetAsync("user-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task FocusStateOverride_SetAndGet_RoundTrips()
    {
        var store = new EfFocusStateOverrideStore(_db);

        await store.SetAsync("user-1", FocusStateType.DeepWork);
        var result = await store.GetAsync("user-1");

        Assert.Equal(FocusStateType.DeepWork, result);
    }

    [Fact]
    public async Task FocusStateOverride_SetAsync_OverwritesPreviousValue()
    {
        var store = new EfFocusStateOverrideStore(_db);

        await store.SetAsync("user-1", FocusStateType.DeepWork);
        await store.SetAsync("user-1", FocusStateType.Away);
        var result = await store.GetAsync("user-1");

        Assert.Equal(FocusStateType.Away, result);
    }

    [Fact]
    public async Task FocusStateOverride_ClearAsync_RemovesOverride()
    {
        var store = new EfFocusStateOverrideStore(_db);

        await store.SetAsync("user-1", FocusStateType.DeepWork);
        await store.ClearAsync("user-1");
        var result = await store.GetAsync("user-1");

        Assert.Null(result);
    }

    #endregion

    #region 2.2 EfInterruptionDecisionStore

    [Fact]
    public async Task InterruptionDecision_RecordAndQuery_RoundTrips()
    {
        var store = new EfInterruptionDecisionStore(_db);
        var record = new InterruptionDecisionRecord(
            WorkItemId: Guid.NewGuid(),
            Title: "Test item",
            SourceType: "TeamsMessage",
            Decision: "INTERRUPT",
            PriorityScore: 80,
            Explanation: "High priority",
            Timestamp: DateTimeOffset.UtcNow,
            FocusState: "DeepWork");

        await store.RecordAsync(record);
        var result = await store.QueryAsync(1, 10);

        Assert.Single(result.Items);
        Assert.Equal("Test item", result.Items[0].Title);
        Assert.Equal("INTERRUPT", result.Items[0].Decision);
        Assert.Equal(80, result.Items[0].PriorityScore);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task InterruptionDecision_QueryAsync_PaginatesCorrectly()
    {
        var store = new EfInterruptionDecisionStore(_db);

        for (var i = 0; i < 5; i++)
        {
            var record = new InterruptionDecisionRecord(
                WorkItemId: Guid.NewGuid(),
                Title: $"Item {i}",
                SourceType: "TeamsMessage",
                Decision: "QUEUE",
                PriorityScore: null,
                Explanation: $"Explanation {i}",
                Timestamp: DateTimeOffset.UtcNow.AddMinutes(i),
                FocusState: "WindowOfOpportunity");
            await store.RecordAsync(record);
        }

        var page1 = await store.QueryAsync(1, 2);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(1, page1.Page);

        var page2 = await store.QueryAsync(2, 2);
        Assert.Equal(2, page2.Items.Count);
        Assert.Equal(2, page2.Page);
    }

    #endregion

    #region 2.3 EfAlertRuleStore

    [Fact]
    public async Task AlertRule_GetVipSenders_ReturnsEmpty_WhenNoneAdded()
    {
        var store = new EfAlertRuleStore(_db);

        var result = await store.GetVipSendersAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AlertRule_AddAndGetVipSenders_RoundTrips()
    {
        var store = new EfAlertRuleStore(_db);

        await store.AddVipSenderAsync("boss@company.com", "admin", CancellationToken.None);
        await store.AddVipSenderAsync("ceo@company.com", "admin", CancellationToken.None);
        var result = await store.GetVipSendersAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains("boss@company.com", result);
        Assert.Contains("ceo@company.com", result);
    }

    [Fact]
    public async Task AlertRule_RemoveVipSender_RemovesEntry()
    {
        var store = new EfAlertRuleStore(_db);

        await store.AddVipSenderAsync("boss@company.com", "admin", CancellationToken.None);
        await store.RemoveVipSenderAsync("boss@company.com", CancellationToken.None);
        var result = await store.GetVipSendersAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AlertRule_AddAndGetKeywords_RoundTrips()
    {
        var store = new EfAlertRuleStore(_db);

        await store.AddKeywordAsync("urgent", "admin", CancellationToken.None);
        await store.AddKeywordAsync("critical", "admin", CancellationToken.None);
        var result = await store.GetKeywordsAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains("urgent", result);
        Assert.Contains("critical", result);
    }

    [Fact]
    public async Task AlertRule_RemoveKeyword_RemovesEntry()
    {
        var store = new EfAlertRuleStore(_db);

        await store.AddKeywordAsync("urgent", "admin", CancellationToken.None);
        await store.RemoveKeywordAsync("urgent", CancellationToken.None);
        var result = await store.GetKeywordsAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AlertRule_AddVipSender_IsIdempotent()
    {
        var store = new EfAlertRuleStore(_db);

        await store.AddVipSenderAsync("boss@company.com", "admin", CancellationToken.None);
        await store.AddVipSenderAsync("boss@company.com", "admin", CancellationToken.None);
        var result = await store.GetVipSendersAsync(CancellationToken.None);

        Assert.Single(result);
    }

    #endregion

    #region 2.4 EfNotificationOutboxStore

    [Fact]
    public async Task NotificationOutbox_EnqueueAndGetPending_RoundTrips()
    {
        var store = new EfNotificationOutboxStore(_db);
        var entry = new NotificationOutboxEntry(
            workItemId: Guid.NewGuid(),
            userId: "user-1",
            sourceType: "TeamsMessage",
            title: "Test notification",
            priority: 0.8);

        await store.EnqueueAsync(entry, CancellationToken.None);
        var pending = await store.GetPendingAsync(10, CancellationToken.None);

        Assert.Single(pending);
        Assert.Equal("Test notification", pending[0].Title);
        Assert.Equal(0.8, pending[0].Priority);
        Assert.Null(pending[0].DispatchedAt);
    }

    [Fact]
    public async Task NotificationOutbox_MarkDispatched_SetsDispatchedAt()
    {
        var store = new EfNotificationOutboxStore(_db);
        var entry = new NotificationOutboxEntry(
            workItemId: Guid.NewGuid(),
            userId: "user-1",
            sourceType: "TeamsMessage",
            title: "Test",
            priority: 0.5);

        await store.EnqueueAsync(entry, CancellationToken.None);
        await store.MarkDispatchedAsync(entry.Id, CancellationToken.None);
        var pending = await store.GetPendingAsync(10, CancellationToken.None);

        Assert.Empty(pending);
    }

    [Fact]
    public async Task NotificationOutbox_GetPending_OrdersByPriorityDescThenCreatedAtAsc()
    {
        var store = new EfNotificationOutboxStore(_db);

        var low = new NotificationOutboxEntry(
            workItemId: Guid.NewGuid(), userId: "u", sourceType: "s",
            title: "Low", priority: 0.1);
        var high = new NotificationOutboxEntry(
            workItemId: Guid.NewGuid(), userId: "u", sourceType: "s",
            title: "High", priority: 0.9);

        await store.EnqueueAsync(low, CancellationToken.None);
        await store.EnqueueAsync(high, CancellationToken.None);
        var pending = await store.GetPendingAsync(10, CancellationToken.None);

        Assert.Equal("High", pending[0].Title);
        Assert.Equal("Low", pending[1].Title);
    }

    #endregion

    #region 2.5 EfMeetingAlertStore

    [Fact]
    public async Task MeetingAlert_GetUnsentAlert_ReturnsNull_WhenNoneExist()
    {
        var store = new EfMeetingAlertStore(_db);

        var result = await store.GetUnsentAlertAsync("evt-1", MeetingAlertTrigger.TenMinutes,
            DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task MeetingAlert_MarkSentAndRetrieve_RoundTrips()
    {
        var store = new EfMeetingAlertStore(_db);
        var alert = new MeetingAlert(
            "evt-1", "Standup", MeetingAlertTrigger.TenMinutes,
            DateTimeOffset.UtcNow, "https://meet.example.com", "user-1");

        await store.MarkSentAsync(alert, CancellationToken.None);
        var upcoming = await store.GetUpcomingAlertsAsync(
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddHours(1),
            CancellationToken.None);

        Assert.Single(upcoming);
        Assert.Equal("Standup", upcoming[0].Title);
        Assert.Equal("evt-1", upcoming[0].EventId);
    }

    [Fact]
    public async Task MeetingAlert_GetUpcomingAlerts_FiltersByDateRange()
    {
        var store = new EfMeetingAlertStore(_db);
        var now = DateTimeOffset.UtcNow;

        var inRange = new MeetingAlert(
            "evt-1", "Meeting A", MeetingAlertTrigger.FiveMinutes,
            now.AddMinutes(30), null, "user-1");
        var outOfRange = new MeetingAlert(
            "evt-2", "Meeting B", MeetingAlertTrigger.SixtyMinutes,
            now.AddHours(5), null, "user-1");

        await store.MarkSentAsync(inRange, CancellationToken.None);
        await store.MarkSentAsync(outOfRange, CancellationToken.None);

        var upcoming = await store.GetUpcomingAlertsAsync(
            now.AddHours(-1), now.AddHours(1), CancellationToken.None);

        Assert.Single(upcoming);
        Assert.Equal("Meeting A", upcoming[0].Title);
    }

    #endregion

    #region 2.6 EfMorningSummaryEmissionStore

    [Fact]
    public async Task MorningSummaryEmission_HasBeenEmitted_ReturnsFalse_WhenNotEmitted()
    {
        var store = new EfMorningSummaryEmissionStore(_db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = await store.HasBeenEmittedAsync("user-1", today, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task MorningSummaryEmission_MarkEmittedAndCheck_ReturnsTrue()
    {
        var store = new EfMorningSummaryEmissionStore(_db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await store.MarkEmittedAsync("user-1", today, CancellationToken.None);
        var result = await store.HasBeenEmittedAsync("user-1", today, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task MorningSummaryEmission_Reset_ClearsEmission()
    {
        var store = new EfMorningSummaryEmissionStore(_db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await store.MarkEmittedAsync("user-1", today, CancellationToken.None);
        await store.ResetAsync("user-1", today, CancellationToken.None);
        var result = await store.HasBeenEmittedAsync("user-1", today, CancellationToken.None);

        Assert.False(result);
    }

    #endregion

    #region 2.7 EfWorkItemStore

    [Fact]
    public async Task WorkItem_SaveAndFindByExternalId_RoundTrips()
    {
        var store = new EfWorkItemStore(_db);
        var item = new WorkItem(
            externalId: "ext-1",
            title: "Test work item",
            source: "teams",
            sourceType: WorkItemSourceType.TeamsMessage,
            priority: WorkItemPriority.High,
            metadata: new Dictionary<string, string> { ["channel"] = "general" });

        var saveResult = await store.SaveAsync(item, CancellationToken.None);
        Assert.True(saveResult.IsSuccess);

        var found = await store.FindByExternalIdAsync("ext-1", CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal("Test work item", found.Title);
        Assert.Equal(WorkItemPriority.High, found.Priority);
    }

    [Fact]
    public async Task WorkItem_FindByExternalId_ReturnsNull_WhenNotFound()
    {
        var store = new EfWorkItemStore(_db);

        var found = await store.FindByExternalIdAsync("nonexistent", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task WorkItem_SaveAsync_UpsertsOnExternalIdConflict()
    {
        var store = new EfWorkItemStore(_db);
        var item = new WorkItem(
            externalId: "ext-1",
            title: "Original",
            source: "teams",
            sourceType: WorkItemSourceType.TeamsMessage,
            priority: WorkItemPriority.Medium,
            metadata: new Dictionary<string, string>());

        await store.SaveAsync(item, CancellationToken.None);

        var updated = new WorkItem(
            externalId: "ext-1",
            title: "Updated",
            source: "teams",
            sourceType: WorkItemSourceType.TeamsMessage,
            priority: WorkItemPriority.High,
            metadata: new Dictionary<string, string>());

        await store.SaveAsync(updated, CancellationToken.None);
        var found = await store.FindByExternalIdAsync("ext-1", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("Updated", found.Title);
    }

    [Fact]
    public async Task WorkItem_GetPendingExternalIds_ReturnsOnlyPendingForSource()
    {
        var store = new EfWorkItemStore(_db);
        var pending = new WorkItem(
            externalId: "ext-pending",
            title: "Pending",
            source: "teams",
            sourceType: WorkItemSourceType.TeamsMessage,
            priority: WorkItemPriority.Medium,
            metadata: new Dictionary<string, string>());
        var completed = new WorkItem(
            externalId: "ext-completed",
            title: "Completed",
            source: "teams",
            sourceType: WorkItemSourceType.TeamsMessage,
            priority: WorkItemPriority.Low,
            metadata: new Dictionary<string, string>());

        await store.SaveAsync(pending, CancellationToken.None);
        await store.SaveAsync(completed, CancellationToken.None);
        await store.MarkCompletedAsync(
            new HashSet<string> { "ext-completed" },
            WorkItemSourceType.TeamsMessage,
            CancellationToken.None);

        var ids = await store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);

        Assert.Single(ids);
        Assert.Contains("ext-pending", ids);
    }

    [Fact]
    public async Task WorkItem_MarkCompletedAsync_UpdatesMatchingItems()
    {
        var store = new EfWorkItemStore(_db);
        var item1 = new WorkItem("ext-1", "Item 1", "teams", WorkItemSourceType.TeamsMessage,
            WorkItemPriority.Medium, new Dictionary<string, string>());
        var item2 = new WorkItem("ext-2", "Item 2", "teams", WorkItemSourceType.TeamsMessage,
            WorkItemPriority.Low, new Dictionary<string, string>());

        await store.SaveAsync(item1, CancellationToken.None);
        await store.SaveAsync(item2, CancellationToken.None);
        await store.MarkCompletedAsync(
            new HashSet<string> { "ext-1", "ext-2" },
            WorkItemSourceType.TeamsMessage,
            CancellationToken.None);

        var pending = await store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);

        Assert.Empty(pending);
    }

    #endregion

    #region 2.8 EfSemanticOutboxRepository

    [Fact]
    public async Task SemanticOutbox_EnqueueAndFetchPending_RoundTrips()
    {
        var store = new EfSemanticOutboxRepository(_db);
        var entry = new SemanticOutboxEntry(
            Guid.NewGuid(), "source-1", "Test content",
            SemanticCollectionType.ActivityMemory, DateTimeOffset.UtcNow);

        await store.EnqueueAsync(entry, CancellationToken.None);
        var pending = await store.FetchPendingAsync(10, CancellationToken.None);

        Assert.Single(pending);
        Assert.Equal("source-1", pending[0].CanonicalSourceId);
        Assert.Equal("Test content", pending[0].Content);
        Assert.Equal(SemanticCollectionType.ActivityMemory, pending[0].Collection);
        Assert.False(pending[0].Processed);
    }

    [Fact]
    public async Task SemanticOutbox_UpdateAsync_MarksAsProcessed()
    {
        var store = new EfSemanticOutboxRepository(_db);
        var entry = new SemanticOutboxEntry(
            Guid.NewGuid(), "source-1", "Content",
            SemanticCollectionType.ProjectKnowledge, DateTimeOffset.UtcNow);

        await store.EnqueueAsync(entry, CancellationToken.None);
        entry.MarkProcessed();
        await store.UpdateAsync(entry, CancellationToken.None);

        var pending = await store.FetchPendingAsync(10, CancellationToken.None);
        Assert.Empty(pending);
    }

    [Fact]
    public async Task SemanticOutbox_FetchPending_ReturnsOnlyUnprocessed()
    {
        var store = new EfSemanticOutboxRepository(_db);

        var processed = new SemanticOutboxEntry(
            Guid.NewGuid(), "source-1", "Processed",
            SemanticCollectionType.ActivityMemory, DateTimeOffset.UtcNow.AddMinutes(-2));
        var unprocessed = new SemanticOutboxEntry(
            Guid.NewGuid(), "source-2", "Unprocessed",
            SemanticCollectionType.ActivityMemory, DateTimeOffset.UtcNow.AddMinutes(-1));

        await store.EnqueueAsync(processed, CancellationToken.None);
        await store.EnqueueAsync(unprocessed, CancellationToken.None);

        processed.MarkProcessed();
        await store.UpdateAsync(processed, CancellationToken.None);

        var pending = await store.FetchPendingAsync(10, CancellationToken.None);

        Assert.Single(pending);
        Assert.Equal("source-2", pending[0].CanonicalSourceId);
    }

    #endregion

    #region 2.9 EfMsalTokenCacheStore

    [Fact]
    public async Task MsalTokenCache_Retrieve_ReturnsNull_WhenNoCacheExists()
    {
        var store = new EfMsalTokenCacheStore(_db);

        var result = await store.RetrieveAsync("nonexistent-key");

        Assert.Null(result);
    }

    [Fact]
    public async Task MsalTokenCache_PersistAndRetrieve_RoundTrips()
    {
        var store = new EfMsalTokenCacheStore(_db);
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        await store.PersistAsync("cache-key-1", data);
        var result = await store.RetrieveAsync("cache-key-1");

        Assert.NotNull(result);
        Assert.Equal(data, result);
    }

    [Fact]
    public async Task MsalTokenCache_HasCachedData_ReturnsTrueAfterPersist()
    {
        var store = new EfMsalTokenCacheStore(_db);
        var data = new byte[] { 0xAB, 0xCD };

        await store.PersistAsync("cache-key-2", data);
        var hasData = await store.HasCachedDataAsync("cache-key-2");

        Assert.True(hasData);
    }

    [Fact]
    public async Task MsalTokenCache_HasCachedData_ReturnsFalse_WhenAbsent()
    {
        var store = new EfMsalTokenCacheStore(_db);

        var hasData = await store.HasCachedDataAsync("missing-key");

        Assert.False(hasData);
    }

    [Fact]
    public async Task MsalTokenCache_PersistAsync_UpsertsOnConflict()
    {
        var store = new EfMsalTokenCacheStore(_db);
        var data1 = new byte[] { 0x01 };
        var data2 = new byte[] { 0x02, 0x03 };

        await store.PersistAsync("key", data1);
        await store.PersistAsync("key", data2);
        var result = await store.RetrieveAsync("key");

        Assert.Equal(data2, result);
    }

    #endregion
}
