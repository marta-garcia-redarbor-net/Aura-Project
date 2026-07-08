namespace Aura.Application.Ports;

/// <summary>
/// Notifies interested clients that dashboard data changed and should be refreshed.
/// This is an application-level port so production ingestion and demo flows can reuse the same contract.
/// </summary>
public interface IDashboardRefreshDispatcher
{
    Task DispatchAsync(string? userId, CancellationToken ct);
}
