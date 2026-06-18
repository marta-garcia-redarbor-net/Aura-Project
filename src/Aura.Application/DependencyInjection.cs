using Aura.Application.Kernel;
using Aura.Application.Kernel.Plugins;
using Aura.Application.Ports;
using Aura.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.Application;

/// <summary>
/// DI registration for Application-layer services.
/// Provides a single entry point for host applications to register all application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Application-layer services (chunk extractor, kernel pipeline, etc.) with the DI container.
    /// </summary>
    public static IServiceCollection AddAuraApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISemanticChunkExtractor, BasicSemanticChunkExtractor>();
        services.AddScoped<IInitialDashboardReader, InitialDashboardReader>();
        services.AddScoped<IGraphConnectorStatusReader, GraphConnectorStatusReader>();
        services.AddScoped<ISystemStatusReader, SystemStatusReader>();
        services.AddScoped<IModuleProgressReader, ModuleProgressReader>();

        AddKernel(services);

        return services;
    }

    /// <summary>
    /// Registers kernel pipeline components. Isolated to reduce merge risk with parallel changes.
    /// </summary>
    private static void AddKernel(IServiceCollection services)
    {
        services.AddSingleton<IPlugin, HelloPlugin>();
        services.AddSingleton<IPluginRegistry, PluginRegistry>();
    }
}
