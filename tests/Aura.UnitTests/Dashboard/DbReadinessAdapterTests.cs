using Aura.Application.Models;
using Aura.Infrastructure.Adapters.Dashboard;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace Aura.UnitTests.Dashboard;

public class DbReadinessAdapterTests
{
    [Fact]
    public async Task GetReadinessAsync_WhenDatabaseHealthy_ReturnsHealthy()
    {
        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService.CheckHealthAsync(
                Arg.Any<Func<HealthCheckRegistration, bool>>(),
                Arg.Any<CancellationToken>())
            .Returns(new HealthReport(new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new(HealthStatus.Healthy, "", TimeSpan.Zero, null, null)
            }, TimeSpan.Zero));

        var adapter = new DbReadinessAdapter(healthCheckService);

        var result = await adapter.GetReadinessAsync(CancellationToken.None);

        Assert.Equal(ReadinessSignal.Healthy, result);
    }

    [Fact]
    public async Task GetReadinessAsync_WhenDatabaseDegraded_ReturnsDegraded()
    {
        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService.CheckHealthAsync(
                Arg.Any<Func<HealthCheckRegistration, bool>>(),
                Arg.Any<CancellationToken>())
            .Returns(new HealthReport(new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new(HealthStatus.Degraded, "", TimeSpan.Zero, null, null)
            }, TimeSpan.Zero));

        var adapter = new DbReadinessAdapter(healthCheckService);

        var result = await adapter.GetReadinessAsync(CancellationToken.None);

        Assert.Equal(ReadinessSignal.Degraded, result);
    }

    [Fact]
    public async Task GetReadinessAsync_WhenDatabaseUnhealthy_ReturnsUnavailable()
    {
        var healthCheckService = Substitute.For<HealthCheckService>();
        healthCheckService.CheckHealthAsync(
                Arg.Any<Func<HealthCheckRegistration, bool>>(),
                Arg.Any<CancellationToken>())
            .Returns(new HealthReport(new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new(HealthStatus.Unhealthy, "", TimeSpan.Zero, null, null)
            }, TimeSpan.Zero));

        var adapter = new DbReadinessAdapter(healthCheckService);

        var result = await adapter.GetReadinessAsync(CancellationToken.None);

        Assert.Equal(ReadinessSignal.Unavailable, result);
    }

    [Fact]
    public async Task GetReadinessAsync_FiltersByDatabaseName()
    {
        var healthCheckService = Substitute.For<HealthCheckService>();
        Func<HealthCheckRegistration, bool>? capturedPredicate = null;

        healthCheckService.CheckHealthAsync(
                Arg.Do<Func<HealthCheckRegistration, bool>>(p => capturedPredicate = p),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var predicate = callInfo.Arg<Func<HealthCheckRegistration, bool>>();
                return new HealthReport(new Dictionary<string, HealthReportEntry>
                {
                    ["database"] = new(HealthStatus.Healthy, "", TimeSpan.Zero, null, null)
                }, TimeSpan.Zero);
            });

        var adapter = new DbReadinessAdapter(healthCheckService);

        await adapter.GetReadinessAsync(CancellationToken.None);

        Assert.NotNull(capturedPredicate);

        var matchingReg = new HealthCheckRegistration("database", _ => Substitute.For<IHealthCheck>(), null, null);
        var nonMatchingReg = new HealthCheckRegistration("qdrant", _ => Substitute.For<IHealthCheck>(), null, null);

        Assert.True(capturedPredicate(matchingReg));
        Assert.False(capturedPredicate(nonMatchingReg));
    }
}
