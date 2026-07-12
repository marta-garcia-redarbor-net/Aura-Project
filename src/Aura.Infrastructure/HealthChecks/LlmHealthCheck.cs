using Aura.Infrastructure.Adapters.LlmAdvisor;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Aura.Infrastructure.HealthChecks;

/// <summary>
/// ASP.NET Core health check that verifies LLM (Ollama) connectivity
/// via HTTP GET /api/tags with a 3-second timeout.
///
/// Uses the same dual-constructor pattern as QdrantHealthCheck:
/// a production constructor that uses IHttpClientFactory and an
/// internal testing constructor that accepts a delegate.
/// </summary>
internal sealed class LlmHealthCheck : IHealthCheck
{
    private readonly Func<CancellationToken, Task<bool>> _healthProbe;

    /// <summary>
    /// Production constructor — performs HTTP GET to Ollama /api/tags.
    /// Gracefully degrades when no endpoint is configured (returns Unhealthy).
    /// </summary>
    public LlmHealthCheck(IHttpClientFactory httpClientFactory, IOptions<LlmAdvisorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);

        var baseUrl = options.Value.Endpoint?.TrimEnd('/');

        _healthProbe = async ct =>
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                return false; // No LLM endpoint configured → Unhealthy
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            var client = httpClientFactory.CreateClient("llm-health");
            var response = await client.GetAsync($"{baseUrl}/api/tags", cts.Token);
            return response.IsSuccessStatusCode;
        };
    }

    /// <summary>
    /// Testing constructor — allows injecting a fake health probe delegate.
    /// </summary>
    internal LlmHealthCheck(Func<CancellationToken, Task<bool>> healthProbe)
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
                ? HealthCheckResult.Healthy("LLM (Ollama) is reachable")
                : HealthCheckResult.Unhealthy("LLM /api/tags returned non-success status");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("LLM (Ollama) is unreachable", ex);
        }
    }
}
