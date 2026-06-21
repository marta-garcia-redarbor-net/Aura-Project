using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Teams;
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

        services.AddWorkItems();
        services.AddScoped<IConnectorAdapter, TeamsConnectorAdapter>();

        return services;
    }
}
