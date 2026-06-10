using Aura.Application;
using Aura.Application.Kernel;
using Aura.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aura.UnitTests.Kernel;

public class KernelOnlyStartupTests
{
    [Fact]
    public void KernelOnlyHost_BuildsWithoutInfrastructureConfig()
    {
        // Simulate the kernel-only composition path from Program.cs
        // No EmbeddingProvider, Qdrant, or ConnectionStrings needed
        var builder = Host.CreateApplicationBuilder([]);
        builder.Services.AddAuraApplication();
        builder.Services.AddHostedService<HelloKernelWorker>();

        using var host = builder.Build();

        var registry = host.Services.GetRequiredService<IPluginRegistry>();
        Assert.NotNull(registry);
        Assert.IsType<PluginRegistry>(registry);
    }

    [Fact]
    public void KernelOnlyHost_ResolvesHelloKernelWorker()
    {
        var builder = Host.CreateApplicationBuilder([]);
        builder.Services.AddAuraApplication();
        builder.Services.AddHostedService<HelloKernelWorker>();

        using var host = builder.Build();

        var workers = host.Services.GetServices<IHostedService>();
        Assert.Contains(workers, w => w is HelloKernelWorker);
    }

    [Fact]
    public void KernelOnlyHost_DoesNotRegisterInfrastructureServices()
    {
        var builder = Host.CreateApplicationBuilder([]);
        builder.Services.AddAuraApplication();
        builder.Services.AddHostedService<HelloKernelWorker>();

        using var host = builder.Build();

        // In kernel-only mode, infrastructure workers must NOT be present
        var workers = host.Services.GetServices<IHostedService>();
        Assert.DoesNotContain(workers, w => w is Worker);
        Assert.DoesNotContain(workers, w => w.GetType().Name == "SemanticIndexSyncWorker");
    }
}
