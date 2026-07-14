using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Observability;

/// <summary>
/// DTO for a captured log record.
/// </summary>
public sealed record LogRecordDto(
    LogLevel Level,
    DateTimeOffset Timestamp,
    string CorrelationId,
    string Message,
    string Source);

/// <summary>
/// DTO for a captured activity span.
/// </summary>
public sealed record SpanDto(
    string OperationName,
    double DurationMs,
    DateTimeOffset StartTime,
    string Status,
    IReadOnlyDictionary<string, string?> Tags);

/// <summary>
/// DTO for a captured metric snapshot.
/// </summary>
public sealed record MetricSnapshotDto(
    string Name,
    double Value,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, string?> Tags);
