namespace Aura.Infrastructure.Observability;

/// <summary>
/// Specialized buffer for metric snapshots with default capacity of 100.
/// </summary>
public sealed class MetricSnapshotBuffer : TelemetryBuffer<MetricSnapshotDto>
{
    public MetricSnapshotBuffer(int capacity = 100) : base(capacity) { }
}
