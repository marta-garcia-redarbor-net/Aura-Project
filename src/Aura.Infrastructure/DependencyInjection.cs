using Aura.Application.Ports;
using Aura.Application.UseCases.IngestionSync;
using Aura.Infrastructure.Adapters.Identity;
using Aura.Infrastructure.Adapters.Ingestion;
using Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;
using Aura.Infrastructure.Adapters.Connectors;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.GraphConnector;
using Aura.Infrastructure.Adapters.Dashboard;
using Aura.Infrastructure.Adapters.MorningSummaryScheduling;
using Aura.Infrastructure.Adapters.SeedData;
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
        services.AddConnectorAdapters(configuration);
        services.AddGraphConnectorAdapter(configuration);
        services.AddIdentityAdapter(configuration, environment);
        services.AddDashboardAdapters(configuration, environment);
        services.AddMorningSummarySchedulingAdapters(configuration);

        // Sync infrastructure
        services.AddSingleton<ISyncStateStore, InMemorySyncStateStore>();
        services.AddScoped<TriggerSyncUseCase>(sp => new TriggerSyncUseCase(
            sp.GetServices<IConnectorAdapter>(),
            sp.GetRequiredService<ISyncStateStore>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TriggerSyncUseCase>>(),
            sp.GetRequiredService<IWorkItemBuffer>(),
            sp.GetRequiredService<IWorkItemStore>()));

        services.AddHealthChecks()
            .AddCheck<QdrantHealthCheck>("qdrant");

        AddSeedDataIfDevelopment(services, configuration, environment);

        return services;
    }

    private static void AddSeedDataIfDevelopment(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var seedOptions = new SeedDataOptions();
        configuration.GetSection(SeedDataOptions.SectionName).Bind(seedOptions);

        if (!seedOptions.Enabled)
        {
            return;
        }

        services.Configure<SeedDataOptions>(configuration.GetSection(SeedDataOptions.SectionName));
        services.AddHostedService<SeedDataHostedService>();
    }
}
