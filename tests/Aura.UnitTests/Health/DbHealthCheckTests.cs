using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aura.UnitTests.Health;

/// <summary>
/// Unit tests for <see cref="DbHealthCheck"/>.
/// Uses the internal delegate constructor to inject a fake connection factory.
/// </summary>
public class DbHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenConnectionOpens_ReturnsHealthy()
    {
        // Arrange: factory returns a working in-memory SQLite connection
        var healthCheck = new DbHealthCheck(_ =>
        {
            var conn = new SqliteConnection("Data Source=:memory:");
            conn.Open();
            return Task.FromResult<IDisposable>(conn);
        });

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Contains("reachable", result.Description!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionThrows_ReturnsUnhealthy()
    {
        // Arrange: factory throws (simulates unreachable DB)
        var expectedException = new SqliteException("Connection refused", 14);
        var healthCheck = new DbHealthCheck(_ =>
            Task.FromException<IDisposable>(expectedException));

        // Act
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(), CancellationToken.None);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("unreachable", result.Description!, StringComparison.OrdinalIgnoreCase);
        Assert.Same(expectedException, result.Exception);
    }
}
