using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;

/// <summary>
/// ASP.NET Core health check that verifies connectivity to the Qdrant vector store
/// via HTTP REST GET /healthz on port 6333.
///
/// The Qdrant .NET SDK's QdrantClient.HealthAsync() calls the gRPC endpoint
/// /qdrant.Qdrant/HealthCheck, which returns 404 on recent Qdrant versions.
/// Using the REST endpoint avoids this incompatibility and works correctly
/// regardless of whether gRPC is proxied (e.g. Azure Container Apps).
/// </summary>
internal sealed class QdrantHealthCheck : IHealthCheck
{
    private readonly Func<CancellationToken, Task<bool>> _healthProbe;

    /// <summary>
    /// Production constructor — performs HTTP GET to Qdrant REST /healthz.
    /// </summary>
    public QdrantHealthCheck(IHttpClientFactory httpClientFactory, IOptions<QdrantOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);

        _healthProbe = async ct =>
        {
            var client = httpClientFactory.CreateClient("qdrant-health");
            // ACA internal ingress is always exposed on port 80, regardless of targetPort.
            // The targetPort (6333) is the container port; the ingress DNS name routes to port 80.
            var url = $"http://{options.Value.Host}/healthz";
            var response = await client.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        };
    }

    /// <summary>
    /// Testing constructor — allows injecting a fake health probe delegate.
    /// </summary>
    internal QdrantHealthCheck(Func<CancellationToken, Task<bool>> healthProbe)
    {
        ArgumentNullException.ThrowIfNull(healthProbe);
        _healthProbe = healthProbe;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var healthy = await _healthProbe(cancellationToken);
            return healthy
                ? HealthCheckResult.Healthy("Qdrant is reachable")
                : HealthCheckResult.Unhealthy("Qdrant /healthz returned non-success status");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Qdrant is unreachable", ex);
        }
    }
}
