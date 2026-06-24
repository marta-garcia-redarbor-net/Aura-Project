namespace Aura.Application.Models;

/// <summary>
/// Aggregated result of a sync operation across all sources.
/// </summary>
public sealed record SyncResultDto(IReadOnlyList<SourceSyncResult> Results);

/// <summary>
/// Per-source result of a sync attempt.
/// </summary>
public sealed record SourceSyncResult(
    string Source,
    string Status,
    int ItemCount,
    DateTimeOffset? LastSyncTimestamp,
    string? FailureReason);

/// <summary>
/// Persisted per-source sync state.
/// </summary>
public sealed record SourceSyncState(
    string Source,
    string Status,
    int LastItemCount,
    DateTimeOffset? LastSyncTimestamp);
