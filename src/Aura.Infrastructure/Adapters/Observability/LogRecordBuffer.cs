namespace Aura.Infrastructure.Observability;

/// <summary>
/// Specialized buffer for log records with default capacity of 1000.
/// </summary>
public sealed class LogRecordBuffer : TelemetryBuffer<LogRecordDto>
{
    public LogRecordBuffer(int capacity = 1000) : base(capacity) { }
}
