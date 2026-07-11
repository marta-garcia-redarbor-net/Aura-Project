using Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aura.UnitTests.Health;

/// <summary>
/// Unit tests for <see cref="QdrantHealthCheck"/>.
/// Uses the internal delegate constructor since QdrantClient methods are non-virtual.
/// </summary>
public class QdrantHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenQdrantIsReachable_ReturnsHealthy()
    {
        // Arrange: probe succeeds and returns true
        var healthCheck = new QdrantHealthCheck(_ => Task.FromResult(true));

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("reachable", result.Description!);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenQdrantReturnsNonSuccess_ReturnsUnhealthy()
    {
        // Arrange: probe returns false (e.g. /healthz returned 503)
        var healthCheck = new QdrantHealthCheck(_ => Task.FromResult(false));

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("non-success", result.Description!);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenQdrantThrows_ReturnsUnhealthy()
    {
        // Arrange: probe throws (simulates connection failure)
        var expectedException = new InvalidOperationException("Connection refused");
        var healthCheck = new QdrantHealthCheck(
            _ => Task.FromException<bool>(expectedException));

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("unreachable", result.Description!);
        Assert.Same(expectedException, result.Exception);
    }
}
