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

        // Named HttpClient used by QdrantHealthCheck for REST /healthz calls.
        // The gRPC SDK calls /qdrant.Qdrant/HealthCheck which returns 404 on recent Qdrant versions.
        services.AddHttpClient("qdrant-health")
            .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(5));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
            // Use gRPC on port 6334 via ACA internal ingress (transport: http2, external: false).
            // Internal ACA ingress with http2 supports gRPC without TLS termination issues.
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
