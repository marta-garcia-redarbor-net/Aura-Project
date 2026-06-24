using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Calendar;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.Connectors.Outlook;
using Aura.Infrastructure.Adapters.Connectors.Teams;
using Aura.Infrastructure.Adapters.GraphConnector;
using Aura.Infrastructure.Adapters.WorkItems;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.Infrastructure.Adapters.Connectors;

internal static class DependencyInjection
{
    internal static IServiceCollection AddConnectorAdapters(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddWorkItems(configuration);
        services.AddGraphSourceProviders(configuration);
        services.AddCalendar(configuration);

        // Register source providers for adapter injection when Graph is enabled
        var graphOptions = new GraphConnectorOptions();
        configuration.GetSection(GraphConnectorOptions.SectionName).Bind(graphOptions);

        if (graphOptions.Enabled)
        {
            services.AddScoped<IMessageSourceProvider<TeamsMessageDto>, GraphTeamsSourceProvider>();
            services.AddScoped<IMessageSourceProvider<OutlookEmailDto>, GraphOutlookSourceProvider>();
        }

        services.AddScoped<IConnectorAdapter, TeamsConnectorAdapter>();
        services.AddScoped<IConnectorAdapter, OutlookConnectorAdapter>();

        return services;
    }
}
