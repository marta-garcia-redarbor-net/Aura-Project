using System.Diagnostics;
using System.Diagnostics.Metrics;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.Logging;

namespace Aura.Application.UseCases.ConnectorExecution;

public sealed partial class ExecuteConnectorUseCase
{
    private static readonly ActivitySource ActivitySource = new("Aura.Application.ConnectorExecution");
    private static readonly Meter Meter = new("Aura.Application.ConnectorExecution", "1.0.0");
    private static readonly Counter<int> ExecutionItemsCounter = Meter.CreateCounter<int>("aura.connector.execution.items");

    private readonly IIngestionCheckpointStore _checkpointStore;
    private readonly IReadOnlyList<IConnectorAdapter> _adapters;
    private readonly IWorkItemBuffer _workItemBuffer;
    private readonly IWorkItemStore _workItemStore;
    private readonly ILogger<ExecuteConnectorUseCase> _logger;
    private readonly Func<DateTimeOffset> _utcNow;

    public ExecuteConnectorUseCase(
        IIngestionCheckpointStore checkpointStore,
        IEnumerable<IConnectorAdapter> adapters,
        ILogger<ExecuteConnectorUseCase> logger,
        Func<DateTimeOffset>? utcNow = null)
        : this(
            checkpointStore,
            adapters,
            new NoopWorkItemBuffer(),
            new NoopWorkItemStore(),
            logger,
            utcNow)
    {
    }

    public ExecuteConnectorUseCase(
        IIngestionCheckpointStore checkpointStore,
        IEnumerable<IConnectorAdapter> adapters,
        IWorkItemBuffer workItemBuffer,
        IWorkItemStore workItemStore,
        ILogger<ExecuteConnectorUseCase> logger,
        Func<DateTimeOffset>? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(checkpointStore);
        ArgumentNullException.ThrowIfNull(adapters);
        ArgumentNullException.ThrowIfNull(workItemBuffer);
        ArgumentNullException.ThrowIfNull(workItemStore);
        ArgumentNullException.ThrowIfNull(logger);

        _checkpointStore = checkpointStore;
        _adapters = adapters.ToArray();
        _workItemBuffer = workItemBuffer;
        _workItemStore = workItemStore;
        _logger = logger;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    private sealed class NoopWorkItemBuffer : IWorkItemBuffer
    {
        public void Enqueue(Aura.Domain.WorkItems.WorkItem item)
        {
        }

        public IReadOnlyList<Aura.Domain.WorkItems.WorkItem> Drain()
            => [];
    }

    private sealed class NoopWorkItemStore : IWorkItemStore
    {
        public Task<WorkItemPersistenceResult> SaveAsync(Aura.Domain.WorkItems.WorkItem item, CancellationToken ct)
            => Task.FromResult(WorkItemPersistenceResult.Success());

        public Task<Aura.Domain.WorkItems.WorkItem?> FindByExternalIdAsync(string externalId, CancellationToken ct)
            => Task.FromResult<Aura.Domain.WorkItems.WorkItem?>(null);

        public Task<IReadOnlySet<string>> GetPendingExternalIdsAsync(WorkItemSourceType source, CancellationToken ct)
            => Task.FromResult<IReadOnlySet<string>>(new HashSet<string>());

        public Task MarkCompletedAsync(IReadOnlySet<string> externalIds, WorkItemSourceType source, CancellationToken ct)
            => Task.CompletedTask;
    }

    public async Task<ConnectorExecutionResult> ExecuteAsync(CheckpointIdentity identity, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(identity);

        using var activity = ActivitySource.StartActivity("connector.execution.run", ActivityKind.Internal);
        activity?.SetTag("connector.name", identity.Connector);
        activity?.SetTag("connector.source", identity.Source);
        activity?.SetTag("connector.tenant", identity.Tenant);

        var checkpoint = await _checkpointStore.GetAsync(identity, ct);
        var now = _utcNow();
        var windowStart = checkpoint?.MaxProcessedAt ?? new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        var request = new ConnectorExecutionRequest(identity, windowStart, now);

        // Lightweight Strategy Pattern dispatch: select one provider adapter by canonical connector name.
        var adapter = _adapters.FirstOrDefault(a => string.Equals(a.ConnectorName, identity.Connector, StringComparison.OrdinalIgnoreCase));
        if (adapter is null)
        {
            var failure = new ConnectorExecutionResult(
                identity,
                0,
                ConnectorExecutionStatus.Failure,
                $"No registered connector adapter for '{identity.Connector}'.");

            EmitTelemetry(activity, failure);
            return failure;
        }

        try
        {
            var adapterResult = await adapter.ExecuteAsync(request, ct);
            var (finalResult, batchExternalIds) = await PersistWorkItemsAsync(adapterResult, ct);

            // Diff lifecycle: mark absent pending items as Completed for Outlook
            if (string.Equals(identity.Connector, "outlook", StringComparison.OrdinalIgnoreCase)
                && adapterResult.Status != ConnectorExecutionStatus.Failure)
            {
                await RunDiffLifecycleAsync(WorkItemSourceType.OutlookEmail, batchExternalIds, ct);
            }

            await PersistCheckpointAsync(identity, checkpoint, finalResult, now, ct);
            EmitTelemetry(activity, finalResult);
            return finalResult;
        }
        catch (Exception ex)
        {
            var failure = new ConnectorExecutionResult(
                identity,
                0,
                ConnectorExecutionStatus.Failure,
                ex.Message);

            EmitTelemetry(activity, failure, ex);
            return failure;
        }
    }

    private async Task<(ConnectorExecutionResult Result, IReadOnlySet<string> BatchExternalIds)> PersistWorkItemsAsync(ConnectorExecutionResult adapterResult, CancellationToken ct)
    {
        var bufferedItems = _workItemBuffer.Drain();

        // Capture batch ExternalIds before any consumption
        var batchExternalIds = bufferedItems
            .Select(i => i.ExternalId)
            .ToHashSet(StringComparer.Ordinal);

        if (bufferedItems.Count == 0)
        {
            return (adapterResult, batchExternalIds);
        }

        var failureReasons = new List<string>();

        foreach (var item in bufferedItems)
        {
            var persistResult = await _workItemStore.SaveAsync(item, ct);
            if (!persistResult.IsSuccess)
            {
                failureReasons.Add(persistResult.FailureReason ?? "Unknown persistence failure.");
            }
        }

        if (failureReasons.Count == 0)
        {
            return (adapterResult, batchExternalIds);
        }

        var mergedFailureReason = string.Join(" | ", failureReasons);

        if (adapterResult.Status == ConnectorExecutionStatus.Failure)
        {
            return (adapterResult, batchExternalIds);
        }

        if (adapterResult.Status == ConnectorExecutionStatus.PartialFailure)
        {
            var priorReason = adapterResult.FailureReason ?? "Connector partial failure.";
            return (adapterResult with { FailureReason = $"{priorReason} | Persistence: {mergedFailureReason}" }, batchExternalIds);
        }

        return (adapterResult with
        {
            Status = ConnectorExecutionStatus.PartialFailure,
            FailureReason = $"Persistence: {mergedFailureReason}"
        }, batchExternalIds);
    }

    private async Task RunDiffLifecycleAsync(WorkItemSourceType sourceType, IReadOnlySet<string> batchExternalIds, CancellationToken ct)
    {
        var pendingIds = await _workItemStore.GetPendingExternalIdsAsync(sourceType, ct);
        if (pendingIds.Count == 0)
            return;

        var absentIds = pendingIds.Except(batchExternalIds).ToHashSet(StringComparer.Ordinal);
        if (absentIds.Count == 0)
            return;

        await _workItemStore.MarkCompletedAsync(absentIds, sourceType, ct);
        Log.DiffLifecycleCompleted(_logger, sourceType.ToString(), absentIds.Count);
    }

    private async Task PersistCheckpointAsync(
        CheckpointIdentity identity,
        IngestionCheckpoint? prior,
        ConnectorExecutionResult result,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (result.Status == ConnectorExecutionStatus.Failure)
        {
            return;
        }

        if (result.Status == ConnectorExecutionStatus.PartialFailure)
        {
            if (result.MaxProcessedAt is null)
            {
                return;
            }

            var partialCheckpoint = new IngestionCheckpoint(
                prior?.Cursor,
                result.MaxProcessedAt,
                prior?.ExecutionFinishedAt);

            await _checkpointStore.SaveAsync(identity, partialCheckpoint, ct);
            return;
        }

        var maxProcessedAt = result.ItemCount > 0
            ? result.MaxProcessedAt
            : prior?.MaxProcessedAt;

        var successCheckpoint = new IngestionCheckpoint(
            prior?.Cursor,
            maxProcessedAt,
            now);

        await _checkpointStore.SaveAsync(identity, successCheckpoint, ct);
    }

    private void EmitTelemetry(Activity? activity, ConnectorExecutionResult result, Exception? exception = null)
    {
        var correlationId = activity?.Id ?? Activity.Current?.Id ?? Guid.NewGuid().ToString("N");

        activity?.SetTag("correlation.id", correlationId);
        activity?.SetTag("execution.status", result.Status.ToString());
        activity?.SetTag("execution.item_count", result.ItemCount);

        if ((result.Status == ConnectorExecutionStatus.Failure || result.Status == ConnectorExecutionStatus.PartialFailure)
            && !string.IsNullOrWhiteSpace(result.FailureReason))
        {
            activity?.SetStatus(ActivityStatusCode.Error, result.FailureReason);
        }

        ExecutionItemsCounter.Add(result.ItemCount,
            new KeyValuePair<string, object?>("connector.name", result.Identity.Connector),
            new KeyValuePair<string, object?>("execution.status", result.Status.ToString().ToLowerInvariant()),
            new KeyValuePair<string, object?>("correlation.id", correlationId));

        if (result.Status == ConnectorExecutionStatus.Failure)
        {
            Log.ExecutionFailed(
                _logger,
                result.Identity.Connector,
                result.Identity.Source,
                result.Identity.Tenant,
                result.FailureReason ?? "Unknown failure",
                correlationId,
                exception);

            return;
        }

        if (result.Status == ConnectorExecutionStatus.PartialFailure)
        {
            Log.ExecutionPartiallyFailed(
                _logger,
                result.Identity.Connector,
                result.Identity.Source,
                result.Identity.Tenant,
                result.ItemCount,
                result.FailureReason ?? "Unknown partial failure",
                correlationId);

            return;
        }

        Log.ExecutionSucceeded(
            _logger,
            result.Identity.Connector,
            result.Identity.Source,
            result.Identity.Tenant,
            result.ItemCount,
            correlationId);
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 2201,
            Level = LogLevel.Information,
            Message = "Connector execution succeeded for {Connector}/{Source}/{Tenant} with {ItemCount} items. CorrelationId={CorrelationId}")]
        public static partial void ExecutionSucceeded(
            ILogger logger,
            string connector,
            string source,
            string tenant,
            int itemCount,
            string correlationId);

        [LoggerMessage(
            EventId = 2202,
            Level = LogLevel.Error,
            Message = "Connector execution failed for {Connector}/{Source}/{Tenant}. Reason={FailureReason}. CorrelationId={CorrelationId}")]
        public static partial void ExecutionFailed(
            ILogger logger,
            string connector,
            string source,
            string tenant,
            string failureReason,
            string correlationId,
            Exception? exception);

        [LoggerMessage(
            EventId = 2203,
            Level = LogLevel.Warning,
            Message = "Connector execution partially failed for {Connector}/{Source}/{Tenant}. ItemCount={ItemCount}. Reason={FailureReason}. CorrelationId={CorrelationId}")]
        public static partial void ExecutionPartiallyFailed(
            ILogger logger,
            string connector,
            string source,
            string tenant,
            int itemCount,
            string failureReason,
            string correlationId);

        [LoggerMessage(
            EventId = 2204,
            Level = LogLevel.Information,
            Message = "Diff lifecycle for {SourceType}: marked {Count} absent items as Completed")]
        public static partial void DiffLifecycleCompleted(
            ILogger logger,
            string sourceType,
            int count);
    }
}
