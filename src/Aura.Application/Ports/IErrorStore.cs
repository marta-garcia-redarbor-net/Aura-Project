namespace Aura.Application.Ports;

/// <summary>
/// Represents an error entry with correlation ID for dashboard display.
/// </summary>
public sealed record ErrorEntry(
    string CorrelationId,
    DateTimeOffset Timestamp,
    string Message);

/// <summary>
/// Port for recording and retrieving recent errors for the dashboard.
/// Implementations may use in-memory storage, a ring buffer, or a persistent store.
/// </summary>
public interface IErrorStore
{
    /// <summary>
    /// Records an error entry.
    /// </summary>
    Task RecordAsync(ErrorEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent error entries up to the specified count.
    /// </summary>
    Task<IReadOnlyList<ErrorEntry>> GetRecentAsync(int count, CancellationToken ct = default);
}
