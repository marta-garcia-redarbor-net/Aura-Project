using System.Diagnostics.Metrics;
using Aura.Infrastructure.Observability;

namespace Aura.UnitTests.Adapters.Observability;

public class TelemetryMeterListenerTests
{
    [Fact]
    public void CounterMeasurement_CapturesSnapshot()
    {
        var buffer = new MetricSnapshotBuffer(capacity: 100);
        using var listener = new TelemetryMeterListener(buffer);
        using var meter = new Meter("Test.Meter.Captures");
        var counter = meter.CreateCounter<int>("test.counter.captures");

        counter.Add(5);
        listener.RecordAll();

        var snapshot = buffer.Snapshot().Where(s => s.Name == "test.counter.captures").ToList();
        Assert.Single(snapshot);
        Assert.Equal("test.counter.captures", snapshot[0].Name);
        Assert.Equal(5.0, snapshot[0].Value);
    }

    [Fact]
    public void CounterMeasurement_LongType_CapturesSnapshot()
    {
        var buffer = new MetricSnapshotBuffer(capacity: 100);
        using var listener = new TelemetryMeterListener(buffer);
        using var meter = new Meter("Test.Meter.Long");
        var counter = meter.CreateCounter<long>("test.counter.long");

        counter.Add(42);
        listener.RecordAll();

        var snapshot = buffer.Snapshot().Where(s => s.Name == "test.counter.long").ToList();
        Assert.Single(snapshot);
        Assert.Equal("test.counter.long", snapshot[0].Name);
        Assert.Equal(42.0, snapshot[0].Value);
    }

    [Fact]
    public void CounterMeasurement_DoubleType_CapturesSnapshot()
    {
        var buffer = new MetricSnapshotBuffer(capacity: 100);
        using var listener = new TelemetryMeterListener(buffer);
        using var meter = new Meter("Test.Meter.Double");
        var counter = meter.CreateCounter<double>("test.counter.double");

        counter.Add(3.14);
        listener.RecordAll();

        var snapshot = buffer.Snapshot().Where(s => s.Name == "test.counter.double").ToList();
        Assert.Single(snapshot);
        Assert.Equal(3.14, snapshot[0].Value, precision: 2);
    }

    [Fact]
    public void MultipleMeasurements_AllCaptured()
    {
        var buffer = new MetricSnapshotBuffer(capacity: 100);
        using var listener = new TelemetryMeterListener(buffer);
        using var meter = new Meter("Test.Meter.Multi");
        var counter = meter.CreateCounter<int>("test.counter.multi");

        counter.Add(1);
        counter.Add(2);
        counter.Add(3);
        listener.RecordAll();

        var snapshot = buffer.Snapshot().Where(s => s.Name == "test.counter.multi").ToList();
        Assert.Equal(3, snapshot.Count);
    }

    [Fact]
    public void NonCounterInstrument_NotCaptured()
    {
        var buffer = new MetricSnapshotBuffer(capacity: 100);
        using var listener = new TelemetryMeterListener(buffer);
        using var meter = new Meter("Test.Meter.Gauge");
        var gauge = meter.CreateObservableGauge("test.gauge", () => 42);

        listener.RecordAll();

        var snapshot = buffer.Snapshot().Where(s => s.Name == "test.gauge").ToList();
        Assert.Empty(snapshot);
    }
}
