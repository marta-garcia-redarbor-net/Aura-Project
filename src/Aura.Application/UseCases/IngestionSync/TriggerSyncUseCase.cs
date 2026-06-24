using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Aura.Application.UseCases.IngestionSync;

/// <summary>
/// Triggers sync across all registered connector adapters with per-source result aggregation.
/// Supports partial degradation: if one source fails, others continue independently.
/// </summary>
public sealed partial class TriggerSyncUseCase
{
    private const string AuthRequiredReason = "auth_required";

    private readonly IReadOnlyList<IConnectorAdapter> _adapters;
    private readonly ISyncStateStore _syncStateStore;
    private readonly IWorkItemBuffer? _workItemBuffer;
    private readonly IWorkItemStore? _workItemStore;
    private readonly ILogger<TriggerSyncUseCase> _logger;
    private readonly Func<DateTimeOffset> _utcNow;

    public TriggerSyncUseCase(
        IEnumerable<IConnectorAdapter> adapters,
        ISyncStateStore syncStateStore,
        ILogger<TriggerSyncUseCase> logger,
        Func<DateTimeOffset>? utcNow = null)
        : this(adapters, syncStateStore, logger, null, null, utcNow)
    {
    }

    public TriggerSyncUseCase(
        IEnumerable<IConnectorAdapter> adapters,
        ISyncStateStore syncStateStore,
        ILogger<TriggerSyncUseCase> logger,
        IWorkItemBuffer? workItemBuffer,
        IWorkItemStore? workItemStore,
        Func<DateTimeOffset>? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        ArgumentNullException.ThrowIfNull(syncStateStore);
        ArgumentNullException.ThrowIfNull(logger);

        _adapters = adapters.ToArray();
        _syncStateStore = syncStateStore;
        _workItemBuffer = workItemBuffer;
        _workItemStore = workItemStore;
        _logger = logger;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<SyncResultDto> ExecuteAsync(CancellationToken ct)
    {
        var results = new List<SourceSyncResult>(_adapters.Count);
        var now = _utcNow();

        foreach (var adapter in _adapters)
        {
            var sourceResult = await ExecuteSingleAdapterAsync(adapter, now, ct);
            results.Add(sourceResult);

            var state = new SourceSyncState(
                sourceResult.Source,
                sourceResult.Status,
                sourceResult.ItemCount,
                sourceResult.LastSyncTimestamp);

            await _syncStateStore.UpdateAsync(adapter.ConnectorName, state, ct);
        }

        await PersistBufferedItemsAsync(ct);

        return new SyncResultDto(results);
    }

    private async Task PersistBufferedItemsAsync(CancellationToken ct)
    {
        if (_workItemBuffer is null || _workItemStore is null)
        {
            return;
        }

        var items = _workItemBuffer.Drain();
        foreach (var item in items)
        {
            await _workItemStore.SaveAsync(item, ct);
        }
    }

    private async Task<SourceSyncResult> ExecuteSingleAdapterAsync(
        IConnectorAdapter adapter, DateTimeOffset now, CancellationToken ct)
    {
        var identity = new CheckpointIdentity(adapter.ConnectorName, GetSource(adapter.ConnectorName), "default");
        var request = new ConnectorExecutionRequest(identity, now.AddHours(-1), now);

        try
        {
            var executionResult = await adapter.ExecuteAsync(request, ct);

            var status = executionResult.Status switch
            {
                ConnectorExecutionStatus.Success => "success",
                ConnectorExecutionStatus.PartialFailure => "partial_failure",
                ConnectorExecutionStatus.Failure when IsAuthRequired(executionResult.FailureReason) => "auth_required",
                ConnectorExecutionStatus.Failure => "failure",
                _ => "unknown"
            };

            Log.AdapterCompleted(_logger, adapter.ConnectorName, status, executionResult.ItemCount);

            return new SourceSyncResult(
                adapter.ConnectorName,
                status,
                executionResult.ItemCount,
                executionResult.MaxProcessedAt ?? now,
                executionResult.FailureReason);
        }
        catch (Exception ex)
        {
            var status = IsAuthRequired(ex.Message) ? "auth_required" : "failure";
            Log.AdapterFailed(_logger, adapter.ConnectorName, ex);

            return new SourceSyncResult(
                adapter.ConnectorName,
                status,
                0,
                null,
                ex.Message);
        }
    }

    private static bool IsAuthRequired(string? reason)
        => reason is not null &&
           (reason.Contains("auth_required", StringComparison.OrdinalIgnoreCase) ||
            reason.Contains("no_account", StringComparison.OrdinalIgnoreCase) ||
            reason.Contains("interaction_required", StringComparison.OrdinalIgnoreCase) ||
            reason.Contains("Re-auth", StringComparison.OrdinalIgnoreCase));

    private static string GetSource(string connectorName) =>
        connectorName switch
        {
            "teams" => "messages",
            "outlook" => "inbox",
            "calendar" => "calendar",
            _ => connectorName
        };

    private static partial class Log
    {
        [LoggerMessage(EventId = 5001, Level = LogLevel.Information,
            Message = "TriggerSync adapter {Connector} completed with status={Status}, items={ItemCount}")]
        public static partial void AdapterCompleted(ILogger logger, string connector, string status, int itemCount);

        [LoggerMessage(EventId = 5003, Level = LogLevel.Error,
            Message = "TriggerSync adapter {Connector} failed")]
        public static partial void AdapterFailed(ILogger logger, string connector, Exception exception);
    }
}
