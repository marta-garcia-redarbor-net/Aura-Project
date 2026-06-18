using Aura.Infrastructure.Adapters.Identity;
using Aura.Infrastructure.Adapters.Ingestion;
using Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;
using Aura.Infrastructure.Adapters.GraphConnector;
using Aura.Infrastructure.Adapters.Dashboard;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aura.Infrastructure;

/// <summary>
/// Unified DI registration for all Infrastructure adapters.
/// Provides a single entry point for host applications to register all infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all infrastructure adapter services with the DI container.
    /// Calls internal adapter DI methods for Ingestion (Embedding, SemanticIndex, SemanticOutbox) and Identity.
    /// Also registers the Qdrant health check.
    /// </summary>
    public static IServiceCollection AddAuraInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddIngestionAdapters(configuration);
        services.AddGraphConnectorAdapter(configuration);
        services.AddIdentityAdapter(configuration, environment);
        services.AddDashboardAdapters(configuration, environment);

        services.AddHealthChecks()
            .AddCheck<QdrantHealthCheck>("qdrant");

        return services;
    }
}
