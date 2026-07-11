using Aura.Application.Demo;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Demo;
using Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;
using Aura.Infrastructure.Adapters.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aura.Infrastructure;

/// <summary>
/// DI registration for Demo Mode services.
/// </summary>
public static class DemoModeServiceCollectionExtensions
{
    /// <summary>
    /// Registers Demo Mode services when DemoMode:Enabled is true.
    /// Registers fallback semantic index handlers and DemoService.
    /// </summary>
    public static IServiceCollection AddDemoMode(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var enabled = configuration.GetValue<bool>("DemoMode:Enabled");

        services.Configure<DemoModeOptions>(
            configuration.GetSection(DemoModeOptions.SectionName));

        if (!enabled)
            return services;

        services.TryAddScoped<ISemanticContextRetriever, QdrantFallbackSemanticContextRetriever>();
        services.TryAddScoped<IDecisionContextRetriever, NullDecisionContextRetriever>();
        services.AddScoped<ISemanticIndexWriter, QdrantFallbackSemanticIndexWriter>();
        services.AddScoped<DemoService>();

        return services;
    }
}
