using System.Diagnostics.Metrics;

namespace Aura.UnitTests.TestDoubles.Observability;

internal sealed class MeterCapture : IDisposable
{
    private readonly MeterListener _listener;
    private readonly HashSet<string> _instrumentNames;
    private readonly object _lock = new();

    private readonly List<LongMeasurement> _measurements = [];

    public MeterCapture(string meterName, params string[] instrumentNames)
    {
        _instrumentNames = instrumentNames.ToHashSet(StringComparer.Ordinal);
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (string.Equals(instrument.Meter.Name, meterName, StringComparison.Ordinal)
                && _instrumentNames.Contains(instrument.Name))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            lock (_lock)
            {
                _measurements.Add(new LongMeasurement(instrument.Name, measurement, tags.ToArray()));
            }
        });

        _listener.Start();
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    public IReadOnlyList<LongMeasurement> Snapshot()
    {
        lock (_lock)
        {
            return _measurements.ToList();
        }
    }

    internal sealed record LongMeasurement(string Instrument, long Value, KeyValuePair<string, object?>[] Tags)
    {
        public string? GetTag(string name)
            => Tags.FirstOrDefault(t => string.Equals(t.Key, name, StringComparison.Ordinal)).Value?.ToString();
    }
}
