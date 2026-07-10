using Aura.Infrastructure.Adapters.Ingestion.Embedding;
using Aura.Infrastructure.Adapters.LlmAdvisor;
using Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;
using Aura.Infrastructure.Adapters.Ingestion.SemanticOutbox;
using Aura.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.Infrastructure.Adapters.Ingestion;

/// <summary>
/// DI registration for all Ingestion-bounded-context adapters.
/// Aggregates Embedding, SemanticIndex, and SemanticOutbox under a single entry point.
/// </summary>
internal static class DependencyInjection
{
    internal static IServiceCollection AddIngestionAdapters(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddEmbeddingAdapter(configuration);
        services.AddSemanticIndexAdapter(configuration);
        services.AddLlmDecisionAdvisor(configuration);
        services.AddSemanticOutboxAdapter(configuration);
        services.AddSingleton<IIngestionCheckpointStore, InMemoryIngestionCheckpointStore>();

        return services;
    }
}
