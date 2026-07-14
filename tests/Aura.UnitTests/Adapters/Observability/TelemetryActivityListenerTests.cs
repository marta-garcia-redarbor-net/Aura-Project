using System.Diagnostics;
using Aura.Infrastructure.Observability;

namespace Aura.UnitTests.Adapters.Observability;

public class TelemetryActivityListenerTests : IDisposable
{
    private readonly ActivitySource _testSource;
    private readonly string _sourceName;

    public TelemetryActivityListenerTests()
    {
        // Each test class instance gets a unique ActivitySource to isolate from
        // process-wide activities created by other tests or infrastructure.
        _sourceName = $"Test.Source.{Guid.NewGuid():N}";
        _testSource = new ActivitySource(_sourceName);
    }

    public void Dispose()
    {
        _testSource.Dispose();
    }

    [Fact]
    public void ActivityStopped_CapturesSpanWithOperationName()
    {
        var buffer = new SpanBuffer(capacity: 100);
        using var listener = new TelemetryActivityListener(buffer, _sourceName);

        using (_testSource.StartActivity("Test.Operation"))
        {
            // activity in flight
        }

        var snapshot = buffer.Snapshot();
        Assert.Single(snapshot);
        Assert.Equal("Test.Operation", snapshot[0].OperationName);
    }

    [Fact]
    public void ActivityStopped_CapturesDuration()
    {
        var buffer = new SpanBuffer(capacity: 100);
        using var listener = new TelemetryActivityListener(buffer, _sourceName);

        using (_testSource.StartActivity("Test.Operation"))
        {
            Thread.Sleep(50);
        }

        var snapshot = buffer.Snapshot();
        Assert.Single(snapshot);
        Assert.True(snapshot[0].DurationMs >= 40);
        Assert.True(snapshot[0].DurationMs < 1000);
    }

    [Fact]
    public void ActivityStopped_CapturesTags()
    {
        var buffer = new SpanBuffer(capacity: 100);
        using var listener = new TelemetryActivityListener(buffer, _sourceName);

        using (var activity = _testSource.StartActivity("Test.Operation"))
        {
            activity!.SetTag("http.method", "GET");
            activity.SetTag("http.status_code", "200");
        }

        var snapshot = buffer.Snapshot();
        Assert.Single(snapshot);
        Assert.Equal("GET", snapshot[0].Tags["http.method"]);
        Assert.Equal("200", snapshot[0].Tags["http.status_code"]);
    }

    [Fact]
    public void ActivityStopped_OkStatus_MapsToHealthy()
    {
        var buffer = new SpanBuffer(capacity: 100);
        using var listener = new TelemetryActivityListener(buffer, _sourceName);

        using (var activity = _testSource.StartActivity("Test.Operation"))
        {
            activity!.SetStatus(ActivityStatusCode.Ok);
        }

        var snapshot = buffer.Snapshot();
        Assert.Single(snapshot);
        Assert.Equal("Healthy", snapshot[0].Status);
    }

    [Fact]
    public void ActivityStopped_ErrorStatus_MapsToUnhealthy()
    {
        var buffer = new SpanBuffer(capacity: 100);
        using var listener = new TelemetryActivityListener(buffer, _sourceName);

        using (var activity = _testSource.StartActivity("Test.Operation"))
        {
            activity!.SetStatus(ActivityStatusCode.Error, "Something failed");
        }

        var snapshot = buffer.Snapshot();
        Assert.Single(snapshot);
        Assert.Equal("Unhealthy", snapshot[0].Status);
    }

    [Fact]
    public void MultipleActivities_AllCaptured()
    {
        var buffer = new SpanBuffer(capacity: 100);
        using var listener = new TelemetryActivityListener(buffer, _sourceName);

        using (_testSource.StartActivity("Op1")) { }
        using (_testSource.StartActivity("Op2")) { }
        using (_testSource.StartActivity("Op3")) { }

        var snapshot = buffer.Snapshot();
        Assert.Equal(3, snapshot.Count);
        Assert.Equal("Op1", snapshot[0].OperationName);
        Assert.Equal("Op2", snapshot[1].OperationName);
        Assert.Equal("Op3", snapshot[2].OperationName);
    }
}
