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
    /// Registers Application-layer services (chunk extractor, etc.) with the DI container.
    /// </summary>
    public static IServiceCollection AddAuraApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISemanticChunkExtractor, BasicSemanticChunkExtractor>();

        return services;
    }
}
