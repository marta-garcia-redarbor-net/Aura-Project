using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.AzureDevOps;
using Aura.Infrastructure.Adapters.Connectors.Calendar;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.Connectors.Outlook;
using Aura.Infrastructure.Adapters.Connectors.PrReview;
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

        if (graphOptions.IsProductionReady)
        {
            services.AddScoped<IMessageSourceProvider<TeamsMessageDto>, GraphTeamsSourceProvider>();
            services.AddScoped<IMessageSourceProvider<OutlookEmailDto>, GraphOutlookSourceProvider>();
        }

        services.AddScoped<IConnectorAdapter, TeamsConnectorAdapter>();
        services.AddScoped<IConnectorAdapter, OutlookConnectorAdapter>();

        // Register PR Review connector adapter (v1 — dedicated view, not triage pipeline)
        services.AddScoped<PrReviewWorkItemMapper>();
        services.AddScoped<IConnectorAdapter, PrReviewConnectorAdapter>();

        // Register Azure DevOps source provider gated by PrReview:Enabled
        var prOptions = new AzureDevOpsPrOptions();
        configuration.GetSection(AzureDevOpsPrOptions.SectionName).Bind(prOptions);
        if (prOptions.Enabled && !string.IsNullOrWhiteSpace(prOptions.PatToken))
        {
            services.AddScoped<IMessageSourceProvider<PrReviewDto>, AzureDevOpsPrProvider>();
        }

        return services;
    }
}
