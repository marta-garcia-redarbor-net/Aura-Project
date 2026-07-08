using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aura.Infrastructure.Adapters.Persistence;

/// <summary>
/// ASP.NET Core health check that verifies connectivity to the primary database.
/// Opens a connection and executes a lightweight query to confirm the DB is reachable.
/// </summary>
internal sealed class DbHealthCheck : IHealthCheck
{
    private readonly Func<CancellationToken, Task<IDisposable>> _connectionFactory;

    /// <summary>
    /// Production constructor — creates a real SQLite connection from the provided connection string.
    /// </summary>
    public DbHealthCheck(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        _connectionFactory = async ct =>
        {
            var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString);
            await connection.OpenAsync(ct);
            return connection;
        };
    }

    /// <summary>
    /// Testing constructor — allows injecting a fake connection factory delegate.
    /// </summary>
    internal DbHealthCheck(Func<CancellationToken, Task<IDisposable>> connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory(cancellationToken);
            return HealthCheckResult.Healthy("Database is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is unreachable", ex);
        }
    }
}
