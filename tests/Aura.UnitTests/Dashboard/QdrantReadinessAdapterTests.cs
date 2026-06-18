using Aura.Application.Models;
using Aura.Infrastructure.Adapters.Dashboard;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aura.UnitTests.Dashboard;

public class QdrantReadinessAdapterTests
{
    [Fact]
    public async Task GetReadinessAsync_WhenHealthCheckHealthy_ReturnsHealthy()
    {
        var service = new StubHealthCheckService(HealthStatus.Healthy);
        var adapter = new QdrantReadinessAdapter(service);

        var result = await adapter.GetReadinessAsync(CancellationToken.None);

        Assert.Equal(ReadinessSignal.Healthy, result);
    }

    [Fact]
    public async Task GetReadinessAsync_WhenHealthCheckDegraded_ReturnsDegraded()
    {
        var service = new StubHealthCheckService(HealthStatus.Degraded);
        var adapter = new QdrantReadinessAdapter(service);

        var result = await adapter.GetReadinessAsync(CancellationToken.None);

        Assert.Equal(ReadinessSignal.Degraded, result);
    }

    [Fact]
    public async Task GetReadinessAsync_WhenHealthCheckUnhealthy_ReturnsUnavailable()
    {
        var service = new StubHealthCheckService(HealthStatus.Unhealthy);
        var adapter = new QdrantReadinessAdapter(service);

        var result = await adapter.GetReadinessAsync(CancellationToken.None);

        Assert.Equal(ReadinessSignal.Unavailable, result);
    }

    private sealed class StubHealthCheckService : HealthCheckService
    {
        private readonly HealthStatus _status;

        public StubHealthCheckService(HealthStatus status)
        {
            _status = status;
        }

        public override Task<HealthReport> CheckHealthAsync(
            Func<HealthCheckRegistration, bool>? predicate,
            CancellationToken cancellationToken = default)
        {
            var entries = new Dictionary<string, HealthReportEntry>
            {
                ["qdrant"] = new HealthReportEntry(
                    status: _status,
                    description: "stub",
                    duration: TimeSpan.FromMilliseconds(1),
                    exception: null,
                    data: new Dictionary<string, object>())
            };

            var report = new HealthReport(entries, TimeSpan.FromMilliseconds(1));
            return Task.FromResult(report);
        }
    }
}
