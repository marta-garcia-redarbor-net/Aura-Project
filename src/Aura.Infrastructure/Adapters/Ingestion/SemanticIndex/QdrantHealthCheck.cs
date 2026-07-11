using Microsoft.Extensions.Diagnostics.HealthChecks;
using Qdrant.Client;

namespace Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;

/// <summary>
/// ASP.NET Core health check that verifies connectivity to the Qdrant vector store.
/// Uses the existing <see cref="QdrantClient"/> singleton registered by
/// <see cref="Adapters.Ingestion.SemanticIndex.DependencyInjection.AddSemanticIndexAdapter"/>.
/// </summary>
internal sealed class QdrantHealthCheck : IHealthCheck
{
    private readonly Func<CancellationToken, Task> _healthProbe;

    /// <summary>
    /// Production constructor — resolved by DI with the registered QdrantClient singleton.
    /// </summary>
    public QdrantHealthCheck(QdrantClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _healthProbe = ct => client.HealthAsync(ct);
    }

    /// <summary>
    /// Testing constructor — allows injecting a fake health probe delegate.
    /// </summary>
    internal QdrantHealthCheck(Func<CancellationToken, Task> healthProbe)
    {
        ArgumentNullException.ThrowIfNull(healthProbe);
        _healthProbe = healthProbe;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _healthProbe(cancellationToken);
            return HealthCheckResult.Healthy("Qdrant is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Qdrant is unreachable", ex);
        }
    }
}
