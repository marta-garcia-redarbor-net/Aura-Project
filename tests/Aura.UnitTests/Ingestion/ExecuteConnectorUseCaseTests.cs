using System.Diagnostics;
using System.Diagnostics.Metrics;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.ConnectorExecution;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.UnitTests.Ingestion;

public class ExecuteConnectorUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithRegisteredConnector_ReturnsAdapterResultUnchanged()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var expected = new ConnectorExecutionResult(identity, 5, ConnectorExecutionStatus.Success);
        var adapter = new StubConnectorAdapter("teams", expected);
        var logger = new RecordingLogger<ExecuteConnectorUseCase>();

        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            logger,
            () => DateTimeOffset.Parse("2026-06-19T15:45:00Z"));

        var result = await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnregisteredConnector_ReturnsTypedFailureWithoutThrowing()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var logger = new RecordingLogger<ExecuteConnectorUseCase>();
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { new StubConnectorAdapter("other", new ConnectorExecutionResult(identity, 1, ConnectorExecutionStatus.Success)) },
            logger,
            () => DateTimeOffset.Parse("2026-06-19T15:45:00Z"));

        var result = await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.Equal(ConnectorExecutionStatus.Failure, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingCheckpoint_UsesProcessedAtAsWindowStart()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpointTime = DateTimeOffset.Parse("2026-06-19T10:00:00Z");
        var now = DateTimeOffset.Parse("2026-06-19T15:45:00Z");

        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns(new IngestionCheckpoint("cursor", checkpointTime));

        var adapter = new CapturingConnectorAdapter("teams", new ConnectorExecutionResult(identity, 1, ConnectorExecutionStatus.Success));
        var logger = new RecordingLogger<ExecuteConnectorUseCase>();

        var useCase = new ExecuteConnectorUseCase(checkpointStore, new[] { adapter }, logger, () => now);

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.NotNull(adapter.LastRequest);
        Assert.Equal(checkpointTime, adapter.LastRequest!.WindowStart);
        Assert.Equal(now, adapter.LastRequest.WindowEnd);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutCheckpoint_UsesUtcTodayAsWindowStart()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var now = DateTimeOffset.Parse("2026-06-19T15:45:00Z");

        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var adapter = new CapturingConnectorAdapter("teams", new ConnectorExecutionResult(identity, 1, ConnectorExecutionStatus.Success));
        var logger = new RecordingLogger<ExecuteConnectorUseCase>();

        var useCase = new ExecuteConnectorUseCase(checkpointStore, new[] { adapter }, logger, () => now);

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.NotNull(adapter.LastRequest);
        Assert.Equal(DateTimeOffset.Parse("2026-06-19T00:00:00Z"), adapter.LastRequest!.WindowStart);
        Assert.Equal(now, adapter.LastRequest.WindowEnd);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdapterReturnsFailure_PropagatesTypedFailureWithoutThrowing()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var failure = new ConnectorExecutionResult(identity, 0, ConnectorExecutionStatus.Failure, "adapter failure");
        var adapter = new StubConnectorAdapter("teams", failure);
        var logger = new RecordingLogger<ExecuteConnectorUseCase>();
        var useCase = new ExecuteConnectorUseCase(checkpointStore, new[] { adapter }, logger);

        var result = await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.Equal(failure, result);
    }

    [Fact]
    public void ConnectorExecutionResult_FailureMustIncludeReason()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var result = new ConnectorExecutionResult(identity, 0, ConnectorExecutionStatus.Failure, "failed");

        Assert.Equal(ConnectorExecutionStatus.Failure, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
    }

    [Fact]
    public async Task ExecuteAsync_Success_EmitsCorrelatedTraceMetricAndInfoLog()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var adapter = new StubConnectorAdapter("teams", new ConnectorExecutionResult(identity, 3, ConnectorExecutionStatus.Success));
        var logger = new RecordingLogger<ExecuteConnectorUseCase>();
        var useCase = new ExecuteConnectorUseCase(checkpointStore, new[] { adapter }, logger);

        var activities = new List<Activity>();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Aura.Application.ConnectorExecution",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(activityListener);

        var measurements = new List<MeasurementSample>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Aura.Application.ConnectorExecution" && instrument.Name == "aura.connector.execution.items")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
        {
            measurements.Add(new MeasurementSample(instrument.Name, measurement, tags.ToArray()));
        });
        meterListener.Start();

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.Single(activities);
        Assert.Single(measurements);
        Assert.Single(logger.Entries.Where(e => e.Level == LogLevel.Information));

        var correlationFromActivity = activities[0].Id;
        var correlationFromMetric = measurements[0].GetTag("correlation.id");
        var correlationFromLog = logger.Entries.Single(e => e.Level == LogLevel.Information).Message;

        Assert.False(string.IsNullOrWhiteSpace(correlationFromActivity));
        Assert.False(string.IsNullOrWhiteSpace(correlationFromMetric));
        Assert.Contains(correlationFromActivity!, correlationFromLog, StringComparison.Ordinal);
        Assert.Equal(correlationFromActivity, correlationFromMetric);
        Assert.Equal(3, measurements[0].Value);
    }

    [Fact]
    public async Task ExecuteAsync_Failure_EmitsCorrelatedTraceMetricZeroAndErrorLog()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var adapter = new StubConnectorAdapter("teams", new ConnectorExecutionResult(identity, 0, ConnectorExecutionStatus.Failure, "failed"));
        var logger = new RecordingLogger<ExecuteConnectorUseCase>();
        var useCase = new ExecuteConnectorUseCase(checkpointStore, new[] { adapter }, logger);

        var activities = new List<Activity>();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Aura.Application.ConnectorExecution",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(activityListener);

        var measurements = new List<MeasurementSample>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Aura.Application.ConnectorExecution" && instrument.Name == "aura.connector.execution.items")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<int>((instrument, measurement, tags, state) =>
        {
            measurements.Add(new MeasurementSample(instrument.Name, measurement, tags.ToArray()));
        });
        meterListener.Start();

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.Single(activities);
        Assert.Single(measurements);
        Assert.Single(logger.Entries.Where(e => e.Level == LogLevel.Error));

        var correlationFromActivity = activities[0].Id;
        var correlationFromMetric = measurements[0].GetTag("correlation.id");
        var correlationFromLog = logger.Entries.Single(e => e.Level == LogLevel.Error).Message;

        Assert.False(string.IsNullOrWhiteSpace(correlationFromActivity));
        Assert.False(string.IsNullOrWhiteSpace(correlationFromMetric));
        Assert.Contains(correlationFromActivity!, correlationFromLog, StringComparison.Ordinal);
        Assert.Equal(correlationFromActivity, correlationFromMetric);
        Assert.Equal(0, measurements[0].Value);
    }

    private sealed class StubConnectorAdapter(string connectorName, ConnectorExecutionResult result) : IConnectorAdapter
    {
        public string ConnectorName => connectorName;

        public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
            => Task.FromResult(result);
    }

    private sealed class CapturingConnectorAdapter(string connectorName, ConnectorExecutionResult result) : IConnectorAdapter
    {
        public string ConnectorName => connectorName;
        public ConnectorExecutionRequest? LastRequest { get; private set; }

        public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(result);
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public IList<LogEntry> Entries { get; } = new List<LogEntry>();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    private sealed record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);

    private sealed record MeasurementSample(string Name, int Value, KeyValuePair<string, object?>[] Tags)
    {
        public string? GetTag(string name)
            => Tags.FirstOrDefault(t => string.Equals(t.Key, name, StringComparison.Ordinal)).Value?.ToString();
    }
}
