using System.Diagnostics.Metrics;

namespace Aura.Infrastructure.Observability;

/// <summary>
/// Captures metric snapshots into a ring buffer using MeterListener.
/// Subscribes to all Counter instruments and captures measurements.
/// </summary>
public sealed class TelemetryMeterListener : IDisposable
{
    private readonly MeterListener _listener;
    private readonly MetricSnapshotBuffer _buffer;

    public TelemetryMeterListener(MetricSnapshotBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _buffer = buffer;
        _listener = new MeterListener();
        _listener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
        _listener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _listener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument is Counter<int> or Counter<long> or Counter<double>)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        _listener.Start();
    }

    private void OnMeasurementRecorded<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state) where T : struct
    {
        var tagDict = new Dictionary<string, string?>();
        foreach (var tag in tags)
        {
            tagDict[tag.Key] = tag.Value?.ToString();
        }

        var snapshot = new MetricSnapshotDto(
            instrument.Name,
            Convert.ToDouble(measurement),
            DateTimeOffset.UtcNow,
            tagDict);

        _buffer.Write(snapshot);
    }

    /// <summary>
    /// Forces immediate recording of all pending measurements.
    /// Used in tests to ensure measurements are captured synchronously.
    /// </summary>
    public void RecordAll()
    {
        // MeterListener automatically records measurements as they occur.
        // This method is kept for API compatibility but is a no-op.
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}
