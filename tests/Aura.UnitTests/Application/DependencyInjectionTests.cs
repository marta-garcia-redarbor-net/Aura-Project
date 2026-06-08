using Aura.Application;
using Aura.Application.Ports;
using Aura.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.Application;

public class DependencyInjectionTests
{
    [Fact]
    public void AddAuraApplication_RegistersISemanticChunkExtractor_AsBasicSemanticChunkExtractor()
    {
        var services = new ServiceCollection();

        services.AddAuraApplication();

        using var provider = services.BuildServiceProvider();
        var extractor = provider.GetRequiredService<ISemanticChunkExtractor>();

        Assert.NotNull(extractor);
        Assert.IsType<BasicSemanticChunkExtractor>(extractor);
    }

    [Fact]
    public void AddAuraApplication_RegistersChunkExtractor_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddAuraApplication();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISemanticChunkExtractor));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }
}
