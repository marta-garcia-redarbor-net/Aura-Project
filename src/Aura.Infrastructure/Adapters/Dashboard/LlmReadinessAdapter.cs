using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aura.Infrastructure.Adapters.Dashboard;

internal sealed class LlmReadinessAdapter : ILlmReadinessProvider
{
    private const string LlmCheckName = "llm";
    private readonly HealthCheckService _healthCheckService;

    public LlmReadinessAdapter(HealthCheckService healthCheckService)
    {
        ArgumentNullException.ThrowIfNull(healthCheckService);
        _healthCheckService = healthCheckService;
    }

    public async Task<ReadinessSignal> GetReadinessAsync(CancellationToken cancellationToken)
    {
        var report = await _healthCheckService.CheckHealthAsync(
            registration => string.Equals(registration.Name, LlmCheckName, StringComparison.OrdinalIgnoreCase),
            cancellationToken);

        return report.Status switch
        {
            HealthStatus.Healthy => ReadinessSignal.Healthy,
            HealthStatus.Degraded => ReadinessSignal.Degraded,
            _ => ReadinessSignal.Unavailable
        };
    }
}
