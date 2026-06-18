using Aura.Application.Kernel.Plugins;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aura.UnitTests.Kernel;

public class HelloPluginTests
{
    private readonly HelloPlugin _plugin = new(NullLogger<HelloPlugin>.Instance);

    private static WorkItem CreateWorkItem(string externalId, string title, string source) =>
        new(
            externalId,
            title,
            source,
            WorkItemSourceType.TeamsMessage,
            WorkItemPriority.Low,
            new Dictionary<string, string>
            {
                ["suite"] = "HelloPluginTests"
            },
            "corr-hello",
            new DateTimeOffset(2026, 01, 01, 00, 00, 00, TimeSpan.Zero));

    [Fact]
    public async Task ExecuteAsync_CompletesWithoutMutatingWorkItemState()
    {
        var item = CreateWorkItem("ext-hp-1", "Test Plugin", "unit-test");

        await _plugin.ExecuteAsync(item, CancellationToken.None);

        // HelloPlugin is a no-op plugin — it must not change WorkItem state
        Assert.Equal(WorkItemStatus.Pending, item.Status);
        Assert.Null(item.FaultReason);
    }

    [Fact]
    public async Task ExecuteAsync_HandlesMultipleWorkItemsIndependently()
    {
        var item1 = CreateWorkItem("ext-hp-2", "First Item", "source-a");
        var item2 = CreateWorkItem("ext-hp-3", "Second Item", "source-b");

        await _plugin.ExecuteAsync(item1, CancellationToken.None);
        await _plugin.ExecuteAsync(item2, CancellationToken.None);

        // Both complete independently without state mutation
        Assert.Equal(WorkItemStatus.Pending, item1.Status);
        Assert.Equal(WorkItemStatus.Pending, item2.Status);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new HelloPlugin(null!));
        Assert.Equal("logger", ex.ParamName);
    }
}
