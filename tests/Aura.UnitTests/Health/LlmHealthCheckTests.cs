using Aura.Infrastructure.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aura.UnitTests.Health;

/// <summary>
/// Unit tests for <see cref="LlmHealthCheck"/>.
/// Uses the internal delegate constructor to avoid HTTP dependencies.
/// </summary>
public class LlmHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenOllamaReachable_ReturnsHealthy()
    {
        var healthCheck = new LlmHealthCheck(_ => Task.FromResult(true));

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("reachable", result.Description!);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOllamaReturnsNonSuccess_ReturnsUnhealthy()
    {
        var healthCheck = new LlmHealthCheck(_ => Task.FromResult(false));

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("non-success", result.Description!);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOllamaThrows_ReturnsUnhealthy()
    {
        var expectedException = new InvalidOperationException("Connection refused");
        var healthCheck = new LlmHealthCheck(
            _ => Task.FromException<bool>(expectedException));

        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("unreachable", result.Description!);
        Assert.Same(expectedException, result.Exception);
    }
}
