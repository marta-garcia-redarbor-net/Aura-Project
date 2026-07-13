using System.Diagnostics;

namespace Aura.Infrastructure.Observability;

/// <summary>
/// Captures Activity spans into a ring buffer using ActivityListener.
/// Subscribes to all ActivitySources in the process, or optionally filters by source name.
/// </summary>
public sealed class TelemetryActivityListener : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly SpanBuffer _buffer;

    public TelemetryActivityListener(SpanBuffer buffer, string? sourceName = null)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _buffer = buffer;
        _listener = new ActivityListener
        {
            ShouldListenTo = sourceName is not null
                ? source => source.Name == sourceName
                : _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = OnActivityStarted,
            ActivityStopped = OnActivityStopped
        };
        ActivitySource.AddActivityListener(_listener);
    }

    private void OnActivityStarted(Activity activity)
    {
        // No-op — we capture on stop to get duration
    }

    private void OnActivityStopped(Activity activity)
    {
        var status = activity.Status == ActivityStatusCode.Ok ? "Healthy" : "Unhealthy";
        var tags = activity.Tags.ToDictionary(t => t.Key, t => t.Value);

        var span = new SpanDto(
            activity.OperationName,
            activity.Duration.TotalMilliseconds,
            new DateTimeOffset(activity.StartTimeUtc, TimeSpan.Zero),
            status,
            tags);

        _buffer.Write(span);
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}
