using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aura.Infrastructure.Adapters.Dashboard;

internal sealed class QdrantReadinessAdapter : IQdrantReadinessProvider
{
    private const string QdrantCheckName = "qdrant";
    private readonly HealthCheckService _healthCheckService;

    public QdrantReadinessAdapter(HealthCheckService healthCheckService)
    {
        ArgumentNullException.ThrowIfNull(healthCheckService);
        _healthCheckService = healthCheckService;
    }

    public async Task<ReadinessSignal> GetReadinessAsync(CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(
            registration => string.Equals(registration.Name, QdrantCheckName, StringComparison.OrdinalIgnoreCase),
            cancellationToken);

        return report.Status switch
        {
            HealthStatus.Healthy => ReadinessSignal.Healthy,
            HealthStatus.Degraded => ReadinessSignal.Degraded,
            _ => ReadinessSignal.Unavailable
        };
    }
}
