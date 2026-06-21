using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Teams;
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

        services.AddSingleton<IConnectorAdapter, TeamsConnectorAdapter>();

        return services;
    }
}
