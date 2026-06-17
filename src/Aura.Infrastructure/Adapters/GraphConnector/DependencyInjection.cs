using Aura.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.Infrastructure.Adapters.GraphConnector;

internal static class DependencyInjection
{
    internal static IServiceCollection AddGraphConnectorAdapter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<GraphConnectorOptions>(configuration.GetSection(GraphConnectorOptions.SectionName));
        services.AddScoped<IGraphConnectorSettingsProvider, AppSettingsGraphConnectorSettingsProvider>();

        return services;
    }
}
