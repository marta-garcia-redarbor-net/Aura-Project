using Aura.Application.Kernel;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aura.UnitTests.Kernel;

public class PluginRegistryTests
{
    private readonly ILogger<PluginRegistry> _logger = NullLogger<PluginRegistry>.Instance;

    private static WorkItem CreateWorkItem() =>
        new(
            "ext-001",
            "Test",
            "manual",
            WorkItemSourceType.TeamsMessage,
            WorkItemPriority.Medium,
            new Dictionary<string, string>
            {
                ["suite"] = "PluginRegistryTests"
            },
            "corr-001",
            new DateTimeOffset(2026, 01, 01, 00, 00, 00, TimeSpan.Zero));

    [Fact]
    public async Task ExecuteAsync_SequentialOrder_PluginsExecuteInRegistrationOrder()
    {
        var callOrder = new List<string>();

        var plugin1 = Substitute.For<IPlugin>();
        plugin1.ExecuteAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => { callOrder.Add("plugin1"); return Task.CompletedTask; });

        var plugin2 = Substitute.For<IPlugin>();
        plugin2.ExecuteAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => { callOrder.Add("plugin2"); return Task.CompletedTask; });

        var registry = new PluginRegistry(new[] { plugin1, plugin2 }, _logger);
        var item = CreateWorkItem();

        await registry.ExecuteAsync(item, CancellationToken.None);

        Assert.Equal(new[] { "plugin1", "plugin2" }, callOrder);
        Assert.Equal(WorkItemStatus.Completed, item.Status);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyRegistry_CompletesWithoutModifyingWorkItem()
    {
        var registry = new PluginRegistry(Enumerable.Empty<IPlugin>(), _logger);
        var item = CreateWorkItem();

        await registry.ExecuteAsync(item, CancellationToken.None);

        // Spec: empty registry completes without modifying the WorkItem
        Assert.Equal(WorkItemStatus.Pending, item.Status);
        Assert.Null(item.FaultReason);
    }

    [Fact]
    public async Task ExecuteAsync_PluginThrows_MarksFaultedAndAbortsRemaining()
    {
        var plugin1 = Substitute.For<IPlugin>();
        plugin1.ExecuteAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var plugin2 = Substitute.For<IPlugin>();

        var registry = new PluginRegistry(new[] { plugin1, plugin2 }, _logger);
        var item = CreateWorkItem();

        await registry.ExecuteAsync(item, CancellationToken.None);

        Assert.Equal(WorkItemStatus.Faulted, item.Status);
        Assert.Contains("boom", item.FaultReason);
        await plugin2.DidNotReceive().ExecuteAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_AllPluginsSucceed_MarksCompleted()
    {
        var plugin = Substitute.For<IPlugin>();
        plugin.ExecuteAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var registry = new PluginRegistry(new[] { plugin }, _logger);
        var item = CreateWorkItem();

        await registry.ExecuteAsync(item, CancellationToken.None);

        Assert.Equal(WorkItemStatus.Completed, item.Status);
    }

    [Fact]
    public async Task ExecuteAsync_SecondPluginFails_FirstStillExecuted()
    {
        var callOrder = new List<string>();

        var plugin1 = Substitute.For<IPlugin>();
        plugin1.ExecuteAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => { callOrder.Add("plugin1"); return Task.CompletedTask; });

        var plugin2 = Substitute.For<IPlugin>();
        plugin2.ExecuteAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("fail"));

        var plugin3 = Substitute.For<IPlugin>();

        var registry = new PluginRegistry(new[] { plugin1, plugin2, plugin3 }, _logger);
        var item = CreateWorkItem();

        await registry.ExecuteAsync(item, CancellationToken.None);

        Assert.Single(callOrder); // only plugin1 ran
        Assert.Equal(WorkItemStatus.Faulted, item.Status);
        await plugin3.DidNotReceive().ExecuteAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>());
    }
}
