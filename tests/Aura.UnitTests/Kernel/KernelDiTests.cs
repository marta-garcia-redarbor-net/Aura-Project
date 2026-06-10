using Aura.Application;
using Aura.Application.Kernel;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.Kernel;

public class KernelDiTests
{
    [Fact]
    public void AddAuraApplication_ResolvesIPluginRegistry()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuraApplication();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IPluginRegistry>();

        Assert.NotNull(registry);
        Assert.IsType<PluginRegistry>(registry);
    }

    [Fact]
    public void AddAuraApplication_ResolvesAtLeastOneIPlugin()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuraApplication();

        using var provider = services.BuildServiceProvider();
        var plugins = provider.GetServices<IPlugin>();

        Assert.NotEmpty(plugins);
    }

    [Fact]
    public void AddAuraApplication_RegistersPluginRegistry_AsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuraApplication();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IPluginRegistry));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }
}
