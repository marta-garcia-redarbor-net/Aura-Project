using Aura.Application.Models;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.WorkItems;

namespace Aura.UnitTests.WorkItems;

public class InMemoryWorkItemStoreTests
{
    [Fact]
    public async Task SaveAsync_ValidWorkItem_ReturnsSuccessTypedResult()
    {
        var store = new InMemoryWorkItemStore();
        var item = CreateWorkItem("msg-1");

        var result = await store.SaveAsync(item, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.FailureReason);
    }

    [Fact]
    public async Task FindByExternalIdAsync_ExistingId_ReturnsWorkItem()
    {
        var store = new InMemoryWorkItemStore();
        var item = CreateWorkItem("ext-123");
        await store.SaveAsync(item, CancellationToken.None);

        var found = await store.FindByExternalIdAsync("ext-123", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("ext-123", found!.ExternalId);
    }

    [Fact]
    public async Task FindByExternalIdAsync_NonExistentId_ReturnsNull()
    {
        var store = new InMemoryWorkItemStore();

        var found = await store.FindByExternalIdAsync("nonexistent", CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task SaveAsync_WhenPersistenceFails_ReturnsFailureTypedResult()
    {
        var store = new InMemoryWorkItemStore();
        var item = CreateWorkItem("msg-fail");

        var result = await store.SaveAsync(item, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
    }

    [Fact]
    public async Task SaveAsync_Dedup_ReSaveRetainsOriginalPriority()
    {
        var store = new InMemoryWorkItemStore();
        var first = CreateWorkItem("chat-1", priority: WorkItemPriority.High);
        await store.SaveAsync(first, CancellationToken.None);

        var second = new WorkItem(
            "chat-1",
            "Re-saved title",
            "chats",
            WorkItemSourceType.TeamsChat,
            WorkItemPriority.Low,
            new Dictionary<string, string>());
        await store.SaveAsync(second, CancellationToken.None);

        var stored = await store.FindByExternalIdAsync("chat-1", CancellationToken.None);
        Assert.NotNull(stored);
        // Priority from first save should be retained
        Assert.Equal(WorkItemPriority.High, stored!.Priority);
    }

    [Fact]
    public async Task SaveAsync_Dedup_DifferentExternalId_AreSeparate()
    {
        var store = new InMemoryWorkItemStore();
        var item1 = CreateWorkItem("unique-1");
        var item2 = CreateWorkItem("unique-2");
        await store.SaveAsync(item1, CancellationToken.None);
        await store.SaveAsync(item2, CancellationToken.None);

        var found1 = await store.FindByExternalIdAsync("unique-1", CancellationToken.None);
        var found2 = await store.FindByExternalIdAsync("unique-2", CancellationToken.None);

        Assert.NotNull(found1);
        Assert.NotNull(found2);
        Assert.NotEqual(found1!.Id, found2!.Id);
    }

    // ---- Phase 2: GetPendingExternalIdsAsync ----

    [Fact]
    public async Task GetPendingExternalIdsAsync_ReturnsOnlyPending()
    {
        var store = new InMemoryWorkItemStore();
        var pending = CreateWorkItem("ext-pend");
        var completed = CreateWorkItem("ext-comp");
        completed.MarkProcessing();
        completed.MarkCompleted();

        await store.SaveAsync(pending, CancellationToken.None);
        await store.SaveAsync(completed, CancellationToken.None);

        var result = await store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);

        Assert.Contains("ext-pend", result);
        Assert.DoesNotContain("ext-comp", result);
    }

    [Fact]
    public async Task GetPendingExternalIdsAsync_FiltersBySourceType()
    {
        var store = new InMemoryWorkItemStore();
        var outlook = new WorkItem("ext-ol", "Outlook Item", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium, new Dictionary<string, string>());
        var teams = new WorkItem("ext-teams", "Teams Item", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium, new Dictionary<string, string>());

        await store.SaveAsync(outlook, CancellationToken.None);
        await store.SaveAsync(teams, CancellationToken.None);

        var result = await store.GetPendingExternalIdsAsync(WorkItemSourceType.OutlookEmail, CancellationToken.None);

        Assert.Contains("ext-ol", result);
        Assert.DoesNotContain("ext-teams", result);
    }

    [Fact]
    public async Task GetPendingExternalIdsAsync_NoPending_ReturnsEmptySet()
    {
        var store = new InMemoryWorkItemStore();
        var result = await store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);
        Assert.Empty(result);
    }

    // ---- Phase 2: MarkCompletedAsync ----

    [Fact]
    public async Task MarkCompletedAsync_ChangesMultipleItems()
    {
        var store = new InMemoryWorkItemStore();
        var itemA = CreateWorkItem("ext-a");
        var itemB = CreateWorkItem("ext-b");
        var itemC = CreateWorkItem("ext-c");

        await store.SaveAsync(itemA, CancellationToken.None);
        await store.SaveAsync(itemB, CancellationToken.None);
        await store.SaveAsync(itemC, CancellationToken.None);

        await store.MarkCompletedAsync(new HashSet<string> { "ext-a", "ext-c" }, WorkItemSourceType.TeamsMessage, CancellationToken.None);

        var pending = await store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);
        Assert.DoesNotContain("ext-a", pending);
        Assert.Contains("ext-b", pending);
        Assert.DoesNotContain("ext-c", pending);
    }

    [Fact]
    public async Task MarkCompletedAsync_IgnoresNonexistentIds()
    {
        var store = new InMemoryWorkItemStore();
        var itemA = CreateWorkItem("ext-a");
        await store.SaveAsync(itemA, CancellationToken.None);

        var ex = await Record.ExceptionAsync(() =>
            store.MarkCompletedAsync(new HashSet<string> { "ext-a", "nonexistent" }, WorkItemSourceType.TeamsMessage, CancellationToken.None));

        Assert.Null(ex);

        var pending = await store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);
        Assert.DoesNotContain("ext-a", pending);
    }

    [Fact]
    public async Task MarkCompletedAsync_IncludesSourceFilter()
    {
        var store = new InMemoryWorkItemStore();
        var outlook = new WorkItem("ext-ol", "Outlook Item", "inbox",
            WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium, new Dictionary<string, string>());
        var teams = new WorkItem("ext-teams", "Teams Item", "messages",
            WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium, new Dictionary<string, string>());

        await store.SaveAsync(outlook, CancellationToken.None);
        await store.SaveAsync(teams, CancellationToken.None);

        await store.MarkCompletedAsync(new HashSet<string> { "ext-ol" }, WorkItemSourceType.OutlookEmail, CancellationToken.None);

        var teamsPending = await store.GetPendingExternalIdsAsync(WorkItemSourceType.TeamsMessage, CancellationToken.None);
        Assert.Contains("ext-teams", teamsPending);
    }

    private static WorkItem CreateWorkItem(string externalId, WorkItemPriority priority = WorkItemPriority.Medium) =>
        new(
            externalId,
            $"title-{externalId}",
            "messages",
            WorkItemSourceType.TeamsMessage,
            priority,
            new Dictionary<string, string>());
}
