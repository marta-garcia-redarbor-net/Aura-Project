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
    public async Task SaveAsync_WhenPersistenceFails_ReturnsFailureTypedResult()
    {
        var store = new InMemoryWorkItemStore();
        var item = CreateWorkItem("msg-fail");

        var result = await store.SaveAsync(item, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
    }

    private static WorkItem CreateWorkItem(string externalId) =>
        new(
            externalId,
            $"title-{externalId}",
            "messages",
            WorkItemSourceType.TeamsMessage,
            WorkItemPriority.Medium,
            new Dictionary<string, string>());
}
