using Aura.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qdrant.Client;

namespace Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;

/// <summary>
/// DI registration for the Qdrant semantic index adapter.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Registers Qdrant-backed semantic index services.
    /// Binds <see cref="QdrantOptions"/> from the "Qdrant" configuration section.
    /// </summary>
    internal static IServiceCollection AddSemanticIndexAdapter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<QdrantOptions>(configuration.GetSection(QdrantOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
            return new QdrantClient(
                host: options.Host,
                port: options.GrpcPort,
                apiKey: options.ApiKey);
        });

        services.AddScoped<ISemanticIndexWriter, QdrantSemanticIndexAdapter>();
        services.AddScoped<ISemanticContextRetriever, QdrantSemanticContextAdapter>();
        services.AddScoped<IDecisionContextRetriever, QdrantDecisionContextAdapter>();

        return services;
    }
}
