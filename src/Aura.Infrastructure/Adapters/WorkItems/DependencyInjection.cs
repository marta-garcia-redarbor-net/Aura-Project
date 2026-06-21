using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Outlook;
using Aura.Infrastructure.Adapters.Connectors.Teams;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.Infrastructure.Adapters.WorkItems;

internal static class DependencyInjection
{
    internal static IServiceCollection AddWorkItems(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IWorkItemStore, InMemoryWorkItemStore>();
        services.AddScoped<IWorkItemBuffer, InMemoryWorkItemBuffer>();
        services.AddSingleton<TeamsWorkItemMapper>();
        services.AddSingleton<OutlookWorkItemMapper>();

        return services;
    }
}
