namespace Aura.Infrastructure.Observability;

/// <summary>
/// Specialized buffer for activity spans with default capacity of 500.
/// </summary>
public sealed class SpanBuffer : TelemetryBuffer<SpanDto>
{
    public SpanBuffer(int capacity = 500) : base(capacity) { }
}
