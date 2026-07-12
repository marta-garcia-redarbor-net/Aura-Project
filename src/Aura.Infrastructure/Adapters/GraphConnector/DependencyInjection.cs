using Aura.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aura.Infrastructure.Adapters.GraphConnector;

internal static partial class DependencyInjection
{
    internal static IServiceCollection AddGraphConnectorAdapter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<GraphConnectorOptions>(configuration.GetSection(GraphConnectorOptions.SectionName));
        services.AddScoped<IGraphConnectorSettingsProvider, AppSettingsGraphConnectorSettingsProvider>();

        var options = new GraphConnectorOptions();
        configuration.GetSection(GraphConnectorOptions.SectionName).Bind(options);

        if (options.Enabled)
        {
            var missingFields = GetMissingRequiredFields(options);
            if (missingFields.Count > 0)
            {
                var logger = CreateBootstrapLogger(services);
                Log.MissingRequiredConfigurationFields(logger, string.Join(",", missingFields));
            }
        }

        return services;
    }

    private static List<string> GetMissingRequiredFields(GraphConnectorOptions options)
    {
        var missingFields = new List<string>(capacity: 2);

        if (string.IsNullOrWhiteSpace(options.TenantId) || !Guid.TryParse(options.TenantId, out _))
        {
            missingFields.Add(nameof(GraphConnectorOptions.TenantId));
        }

        if (string.IsNullOrWhiteSpace(options.ClientId) || !Guid.TryParse(options.ClientId, out _))
        {
            missingFields.Add(nameof(GraphConnectorOptions.ClientId));
        }

        return missingFields;
    }

    private static ILogger CreateBootstrapLogger(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetService<ILoggerFactory>();
        return loggerFactory?.CreateLogger("Aura.Infrastructure.GraphConnector")
               ?? NullLogger.Instance;
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 7102,
            Level = LogLevel.Warning,
            Message = "Graph connector enabled with missing required Graph configuration fields: {MissingFields}. Connector status will remain Disabled until these fields are provided.")]
        public static partial void MissingRequiredConfigurationFields(ILogger logger, string missingFields);
    }
}
