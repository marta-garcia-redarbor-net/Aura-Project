using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.ConnectorExecution;
using Aura.Domain.WorkItems;
using Aura.UnitTests.TestDoubles.Observability;
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
            .Returns(new IngestionCheckpoint("cursor", checkpointTime, DateTimeOffset.Parse("2026-06-19T10:03:00Z")));

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
    public async Task ExecuteAsync_FullSuccessWithItems_ReturnsCanonicalResultContract()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var maxProcessedAt = DateTimeOffset.Parse("2026-06-19T14:30:00Z", CultureInfo.InvariantCulture);

        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var expected = new ConnectorExecutionResult(
            identity,
            5,
            ConnectorExecutionStatus.Success,
            FailureReason: null,
            MaxProcessedAt: maxProcessedAt);

        var adapter = new StubConnectorAdapter("teams", expected);
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            new RecordingLogger<ExecuteConnectorUseCase>());

        var result = await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.Equal(identity, result.Identity);
        Assert.Equal(5, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Success, result.Status);
        Assert.Null(result.FailureReason);
        Assert.Equal(maxProcessedAt, result.MaxProcessedAt);
    }

    [Fact]
    public async Task ExecuteAsync_FullSuccessWithoutItems_ReturnsCanonicalResultContract()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");

        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var expected = new ConnectorExecutionResult(
            identity,
            0,
            ConnectorExecutionStatus.Success,
            FailureReason: null,
            MaxProcessedAt: null);

        var adapter = new StubConnectorAdapter("teams", expected);
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            new RecordingLogger<ExecuteConnectorUseCase>());

        var result = await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.Equal(identity, result.Identity);
        Assert.Equal(0, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Success, result.Status);
        Assert.Null(result.FailureReason);
        Assert.Null(result.MaxProcessedAt);
    }

    [Fact]
    public async Task ExecuteAsync_FullFailure_ReturnsCanonicalResultContract()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");

        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var expected = new ConnectorExecutionResult(
            identity,
            0,
            ConnectorExecutionStatus.Failure,
            FailureReason: "adapter failure",
            MaxProcessedAt: null);

        var adapter = new StubConnectorAdapter("teams", expected);
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            new RecordingLogger<ExecuteConnectorUseCase>());

        var result = await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.Equal(identity, result.Identity);
        Assert.Equal(0, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Failure, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
        Assert.Null(result.MaxProcessedAt);
    }

    [Fact]
    public async Task ExecuteAsync_PartialFailure_ReturnsCanonicalResultContract()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var successfulItemsMax = DateTimeOffset.Parse("2026-06-19T14:30:00Z", CultureInfo.InvariantCulture);

        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var expected = new ConnectorExecutionResult(
            identity,
            3,
            ConnectorExecutionStatus.PartialFailure,
            FailureReason: "2 items failed",
            MaxProcessedAt: successfulItemsMax);

        var adapter = new StubConnectorAdapter("teams", expected);
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            new RecordingLogger<ExecuteConnectorUseCase>());

        var result = await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.Equal(identity, result.Identity);
        Assert.Equal(3, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.PartialFailure, result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.FailureReason));
        Assert.Equal(successfulItemsMax, result.MaxProcessedAt);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAdapterThrows_ReturnsTypedFailureContract()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var adapter = new ThrowingConnectorAdapter("teams", new InvalidOperationException("connector crashed"));
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            new RecordingLogger<ExecuteConnectorUseCase>());

        var result = await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.Equal(identity, result.Identity);
        Assert.Equal(0, result.ItemCount);
        Assert.Equal(ConnectorExecutionStatus.Failure, result.Status);
        Assert.Equal("connector crashed", result.FailureReason);
        Assert.Null(result.MaxProcessedAt);
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
        var identity = new CheckpointIdentity("teams", "messages-success-correlation", "acme-success-correlation");
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

        EmitNoiseActivityForConnectorExecutionSource();

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        var correlatedActivities = activities
            .Where(activity =>
                string.Equals(GetActivityTag(activity, "connector.name"), identity.Connector, StringComparison.Ordinal) &&
                string.Equals(GetActivityTag(activity, "connector.source"), identity.Source, StringComparison.Ordinal) &&
                string.Equals(GetActivityTag(activity, "connector.tenant"), identity.Tenant, StringComparison.Ordinal))
            .ToList();

        var correlatedInfoLogs = logger.Entries
            .Where(e =>
                e.Level == LogLevel.Information &&
                e.Message.Contains(identity.Connector, StringComparison.Ordinal) &&
                e.Message.Contains(identity.Source, StringComparison.Ordinal) &&
                e.Message.Contains(identity.Tenant, StringComparison.Ordinal))
            .ToList();

        Assert.Single(correlatedActivities);
        var correlationFromActivity = correlatedActivities[0].Id;

        var correlatedMeasurements = measurements
            .Where(sample =>
                string.Equals(sample.GetTag("correlation.id"), correlationFromActivity, StringComparison.Ordinal) &&
                string.Equals(sample.GetTag("execution.status"), "success", StringComparison.Ordinal))
            .ToList();

        Assert.Single(correlatedMeasurements);
        Assert.Single(correlatedInfoLogs);

        var correlationFromMetric = correlatedMeasurements[0].GetTag("correlation.id");
        var correlationFromLog = correlatedInfoLogs[0].Message;

        Assert.False(string.IsNullOrWhiteSpace(correlationFromActivity));
        Assert.False(string.IsNullOrWhiteSpace(correlationFromMetric));
        Assert.Contains(correlationFromActivity!, correlationFromLog, StringComparison.Ordinal);
        Assert.Equal(correlationFromActivity, correlationFromMetric);
        Assert.Equal(3, correlatedMeasurements[0].Value);
    }

    [Fact]
    public async Task ExecuteAsync_Failure_EmitsCorrelatedTraceMetricZeroAndErrorLog()
    {
        var identity = new CheckpointIdentity("teams", "messages-failure-correlation", "acme-failure-correlation");
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

        EmitNoiseActivityForConnectorExecutionSource();

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        var correlatedActivities = activities
            .Where(activity =>
                string.Equals(GetActivityTag(activity, "connector.name"), identity.Connector, StringComparison.Ordinal) &&
                string.Equals(GetActivityTag(activity, "connector.source"), identity.Source, StringComparison.Ordinal) &&
                string.Equals(GetActivityTag(activity, "connector.tenant"), identity.Tenant, StringComparison.Ordinal))
            .ToList();

        var correlatedErrorLogs = logger.Entries
            .Where(e =>
                e.Level == LogLevel.Error &&
                e.Message.Contains(identity.Connector, StringComparison.Ordinal) &&
                e.Message.Contains(identity.Source, StringComparison.Ordinal) &&
                e.Message.Contains(identity.Tenant, StringComparison.Ordinal))
            .ToList();

        Assert.Single(correlatedActivities);
        var correlationFromActivity = correlatedActivities[0].Id;

        var correlatedMeasurements = measurements
            .Where(sample =>
                string.Equals(sample.GetTag("correlation.id"), correlationFromActivity, StringComparison.Ordinal) &&
                string.Equals(sample.GetTag("execution.status"), "failure", StringComparison.Ordinal))
            .ToList();

        Assert.Single(correlatedMeasurements);
        Assert.Single(correlatedErrorLogs);

        var correlationFromMetric = correlatedMeasurements[0].GetTag("correlation.id");
        var correlationFromLog = correlatedErrorLogs[0].Message;

        Assert.False(string.IsNullOrWhiteSpace(correlationFromActivity));
        Assert.False(string.IsNullOrWhiteSpace(correlationFromMetric));
        Assert.Contains(correlationFromActivity!, correlationFromLog, StringComparison.Ordinal);
        Assert.Equal(correlationFromActivity, correlationFromMetric);
        Assert.Equal(0, correlatedMeasurements[0].Value);
    }

    [Fact]
    public async Task ExecuteAsync_OpensCorrelationScope_BeforeAdapterExecution()
    {
        var identity = new CheckpointIdentity("teams", "messages-scope", "acme-scope");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>()).Returns((IngestionCheckpoint?)null);

        var logger = new ScopeAwareTestLogger<ExecuteConnectorUseCase>();
        string? correlationIdObservedInsideAdapter = null;

        var adapter = new CapturingConnectorAdapter("teams", new ConnectorExecutionResult(identity, 1, ConnectorExecutionStatus.Success))
        {
            OnExecute = _ =>
            {
                if (logger.TryGetCurrentScopeValue("CorrelationId", out var scopeValue))
                {
                    correlationIdObservedInsideAdapter = scopeValue?.ToString();
                }
            }
        };

        var useCase = new ExecuteConnectorUseCase(checkpointStore, new[] { adapter }, logger);

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(correlationIdObservedInsideAdapter));

        var successLog = logger.Entries.Single(e => e.EventId.Id == 2201);
        Assert.Equal(correlationIdObservedInsideAdapter, successLog.Scope["CorrelationId"]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_FullSuccessWithItems_PersistsBothCheckpointTimestamps()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var now = DateTimeOffset.Parse("2026-06-19T15:45:00Z", CultureInfo.InvariantCulture);
        var maxProcessedAt = DateTimeOffset.Parse("2026-06-19T14:30:00Z", CultureInfo.InvariantCulture);

        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var success = new ConnectorExecutionResult(
            identity,
            5,
            ConnectorExecutionStatus.Success,
            MaxProcessedAt: maxProcessedAt);

        var adapter = new StubConnectorAdapter("teams", success);
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            new RecordingLogger<ExecuteConnectorUseCase>(),
            () => now);

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        await checkpointStore.Received(1).SaveAsync(
            identity,
            Arg.Is<IngestionCheckpoint>(c =>
                c.Cursor == null &&
                c.MaxProcessedAt == maxProcessedAt &&
                c.ExecutionFinishedAt == now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_FullSuccessWithoutItems_PersistsOnlyExecutionFinishedAt()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var now = DateTimeOffset.Parse("2026-06-19T16:00:00Z", CultureInfo.InvariantCulture);
        var priorMaxProcessedAt = DateTimeOffset.Parse("2026-06-19T12:00:00Z", CultureInfo.InvariantCulture);

        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns(new IngestionCheckpoint("cursor-v1", priorMaxProcessedAt, null));

        var successWithoutItems = new ConnectorExecutionResult(
            identity,
            0,
            ConnectorExecutionStatus.Success,
            MaxProcessedAt: null);

        var adapter = new StubConnectorAdapter("teams", successWithoutItems);
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            new RecordingLogger<ExecuteConnectorUseCase>(),
            () => now);

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        await checkpointStore.Received(1).SaveAsync(
            identity,
            Arg.Is<IngestionCheckpoint>(c =>
                c.Cursor == "cursor-v1" &&
                c.MaxProcessedAt == priorMaxProcessedAt &&
                c.ExecutionFinishedAt == now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_FullFailure_DoesNotPersistCheckpoint()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");

        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns(new IngestionCheckpoint(
                "cursor-v1",
                DateTimeOffset.Parse("2026-06-19T12:00:00Z", CultureInfo.InvariantCulture),
                DateTimeOffset.Parse("2026-06-19T12:05:00Z", CultureInfo.InvariantCulture)));

        var failure = new ConnectorExecutionResult(identity, 0, ConnectorExecutionStatus.Failure, "adapter failure", null);
        var adapter = new StubConnectorAdapter("teams", failure);
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            new RecordingLogger<ExecuteConnectorUseCase>());

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        await checkpointStore.DidNotReceive().SaveAsync(
            Arg.Any<CheckpointIdentity>(),
            Arg.Any<IngestionCheckpoint>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_PartialFailure_PersistsOnlyMaxProcessedAt()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var priorFinishedAt = DateTimeOffset.Parse("2026-06-19T12:05:00Z", CultureInfo.InvariantCulture);
        var successfulItemsMax = DateTimeOffset.Parse("2026-06-19T14:30:00Z", CultureInfo.InvariantCulture);

        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns(new IngestionCheckpoint("cursor-v1", DateTimeOffset.Parse("2026-06-19T12:00:00Z", CultureInfo.InvariantCulture), priorFinishedAt));

        var partial = new ConnectorExecutionResult(
            identity,
            3,
            ConnectorExecutionStatus.PartialFailure,
            FailureReason: "2 items failed",
            MaxProcessedAt: successfulItemsMax);

        var adapter = new StubConnectorAdapter("teams", partial);
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            new RecordingLogger<ExecuteConnectorUseCase>(),
            () => DateTimeOffset.Parse("2026-06-19T15:45:00Z", CultureInfo.InvariantCulture));

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        await checkpointStore.Received(1).SaveAsync(
            identity,
            Arg.Is<IngestionCheckpoint>(c =>
                c.Cursor == "cursor-v1" &&
                c.MaxProcessedAt == successfulItemsMax &&
                c.ExecutionFinishedAt == priorFinishedAt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_RepeatedRunWithSameWindow_DoesNotRegressCheckpoint()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var maxProcessedAt = DateTimeOffset.Parse("2026-06-19T14:30:00Z", CultureInfo.InvariantCulture);
        var now = DateTimeOffset.Parse("2026-06-19T16:00:00Z", CultureInfo.InvariantCulture);
        IngestionCheckpoint? persisted = null;

        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>())
            .Returns(_ => persisted);

        checkpointStore
            .When(s => s.SaveAsync(identity, Arg.Any<IngestionCheckpoint>(), Arg.Any<CancellationToken>()))
            .Do(callInfo => persisted = callInfo.ArgAt<IngestionCheckpoint>(1));

        var success = new ConnectorExecutionResult(identity, 2, ConnectorExecutionStatus.Success, MaxProcessedAt: maxProcessedAt);
        var adapter = new StubConnectorAdapter("teams", success);
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            new RecordingLogger<ExecuteConnectorUseCase>(),
            () => now);

        await useCase.ExecuteAsync(identity, CancellationToken.None);
        var firstSaved = persisted;

        await useCase.ExecuteAsync(identity, CancellationToken.None);
        var secondSaved = persisted;

        Assert.NotNull(firstSaved);
        Assert.NotNull(secondSaved);
        Assert.True(secondSaved!.MaxProcessedAt >= firstSaved!.MaxProcessedAt);
    }

    [Fact]
    public async Task ExecuteAsync_QueueOrDeferVerdict_DoesNotEnqueueNotification()
    {
        var identity = new CheckpointIdentity("teams", "messages", "acme");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>()).Returns((IngestionCheckpoint?)null);

        var adapter = new StubConnectorAdapter(identity.Connector, new ConnectorExecutionResult(identity, 1, ConnectorExecutionStatus.Success));
        var buffer = Substitute.For<IWorkItemBuffer>();
        buffer.Drain().Returns([CreateBufferedWorkItem(new Dictionary<string, string> { ["assignedTo"] = "user-1" })]);
        var store = Substitute.For<IWorkItemStore>();
        store.SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>()).Returns(WorkItemPersistenceResult.Success());
        var outbox = Substitute.For<INotificationOutboxStore>();
        var dashboardRefresh = Substitute.For<IDashboardRefreshDispatcher>();

        var engine = Substitute.For<IInterruptionPolicyEngine>();
        engine.EvaluateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(
                new InterruptionVerdict(InterruptionDecision.Queue, new EvaluationReport([]), explanation: "queue"),
                new InterruptionVerdict(InterruptionDecision.Defer, new EvaluationReport([]), explanation: "defer"));

        var semanticOutboxRepo = Substitute.For<ISemanticOutboxRepository>();
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            [adapter],
            buffer,
            store,
            engine,
            dashboardRefresh,
            outbox,
            semanticOutboxRepo,
            new RecordingLogger<ExecuteConnectorUseCase>());

        await useCase.ExecuteAsync(identity, CancellationToken.None);
        await useCase.ExecuteAsync(identity, CancellationToken.None);

        await outbox.DidNotReceive().EnqueueAsync(Arg.Any<NotificationOutboxEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_InterruptVerdict_EnqueuesUsingResolvedTargetUser()
    {
        var identity = new CheckpointIdentity("pr", "azdo", "acme");
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(identity, Arg.Any<CancellationToken>()).Returns((IngestionCheckpoint?)null);

        var adapter = new StubConnectorAdapter(identity.Connector, new ConnectorExecutionResult(identity, 1, ConnectorExecutionStatus.Success));
        var buffer = Substitute.For<IWorkItemBuffer>();
        buffer.Drain().Returns([
            CreateBufferedWorkItem(new Dictionary<string, string>
            {
                [WorkItemSignalKeys.TargetResponsibleUserId] = "reviewer-1"
            }, source: "pr")
        ]);
        var store = Substitute.For<IWorkItemStore>();
        store.SaveAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>()).Returns(WorkItemPersistenceResult.Success());
        var outbox = Substitute.For<INotificationOutboxStore>();
        var dashboardRefresh = Substitute.For<IDashboardRefreshDispatcher>();

        var engine = Substitute.For<IInterruptionPolicyEngine>();
        engine.EvaluateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(new InterruptionVerdict(
                InterruptionDecision.InterruptNow,
                new EvaluationReport([]),
                triggerRule: "ExplicitOverrideRule",
                explanation: "override matched",
                targetUserId: "reviewer-1"));

        var semanticOutboxRepo = Substitute.For<ISemanticOutboxRepository>();
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            [adapter],
            buffer,
            store,
            engine,
            dashboardRefresh,
            outbox,
            semanticOutboxRepo,
            new RecordingLogger<ExecuteConnectorUseCase>());

        await useCase.ExecuteAsync(identity, CancellationToken.None);

        await outbox.Received(1).EnqueueAsync(
            Arg.Is<NotificationOutboxEntry>(entry => entry.UserId == "reviewer-1" && entry.SourceType == WorkItemSourceType.PrReview.ToString()),
            Arg.Any<CancellationToken>());
    }

    private static WorkItem CreateBufferedWorkItem(IReadOnlyDictionary<string, string> metadata, string source = "messages")
        => new(
            externalId: Guid.NewGuid().ToString("N"),
            title: "Buffered item",
            source: source,
            sourceType: source == "pr" ? WorkItemSourceType.PrReview : WorkItemSourceType.TeamsMessage,
            priority: WorkItemPriority.High,
            metadata: metadata);

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
        public Action<ConnectorExecutionRequest>? OnExecute { get; init; }

        public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
        {
            LastRequest = request;
            OnExecute?.Invoke(request);
            return Task.FromResult(result);
        }
    }

    private sealed class ThrowingConnectorAdapter(string connectorName, Exception exception) : IConnectorAdapter
    {
        public string ConnectorName => connectorName;

        public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
            => Task.FromException<ConnectorExecutionResult>(exception);
    }

    private static void EmitNoiseActivityForConnectorExecutionSource()
    {
        using var noiseSource = new ActivitySource("Aura.Application.ConnectorExecution");
        using var _ = noiseSource.StartActivity("connector.execution.noise", ActivityKind.Internal);
    }

    private static string? GetActivityTag(Activity activity, string name)
        => activity.Tags.FirstOrDefault(tag => string.Equals(tag.Key, name, StringComparison.Ordinal)).Value;

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
