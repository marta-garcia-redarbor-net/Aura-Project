using Aura.Application.Kernel;
using Aura.Domain.WorkItems;
using Aura.Workers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aura.UnitTests.Kernel;

public class HelloKernelWorkerTests
{
    private readonly IPluginRegistry _registry = Substitute.For<IPluginRegistry>();
    private readonly IHostApplicationLifetime _lifetime = Substitute.For<IHostApplicationLifetime>();
    private readonly ILogger<HelloKernelWorker> _logger = NullLogger<HelloKernelWorker>.Instance;

    [Fact]
    public async Task ExecuteAsync_AfterSuccessfulPipeline_StopsHost()
    {
        _registry.ExecuteAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var worker = new HelloKernelWorker(_registry, _lifetime, _logger);

        await worker.StartAsync(CancellationToken.None);
        // Give the background service a moment to complete
        await Task.Delay(100);
        await worker.StopAsync(CancellationToken.None);

        _lifetime.Received(1).StopApplication();
    }

    [Fact]
    public async Task ExecuteAsync_AfterFailedPipeline_StopsHost()
    {
        _registry.ExecuteAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("pipeline error"));

        var worker = new HelloKernelWorker(_registry, _lifetime, _logger);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await worker.StopAsync(CancellationToken.None);

        // Even on failure, a one-shot worker should stop the host
        _lifetime.Received(1).StopApplication();
    }
}
