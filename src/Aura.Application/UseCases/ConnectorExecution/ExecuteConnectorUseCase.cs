using System.Diagnostics;
using System.Diagnostics.Metrics;
using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Aura.Application.UseCases.ConnectorExecution;

public sealed partial class ExecuteConnectorUseCase
{
    private static readonly ActivitySource ActivitySource = new("Aura.Application.ConnectorExecution");
    private static readonly Meter Meter = new("Aura.Application.ConnectorExecution", "1.0.0");
    private static readonly Counter<int> ExecutionItemsCounter = Meter.CreateCounter<int>("aura.connector.execution.items");

    private readonly IIngestionCheckpointStore _checkpointStore;
    private readonly IReadOnlyList<IConnectorAdapter> _adapters;
    private readonly ILogger<ExecuteConnectorUseCase> _logger;
    private readonly Func<DateTimeOffset> _utcNow;

    public ExecuteConnectorUseCase(
        IIngestionCheckpointStore checkpointStore,
        IEnumerable<IConnectorAdapter> adapters,
        ILogger<ExecuteConnectorUseCase> logger,
        Func<DateTimeOffset>? utcNow = null)
    {
        ArgumentNullException.ThrowIfNull(checkpointStore);
        ArgumentNullException.ThrowIfNull(adapters);
        ArgumentNullException.ThrowIfNull(logger);

        _checkpointStore = checkpointStore;
        _adapters = adapters.ToArray();
        _logger = logger;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<ConnectorExecutionResult> ExecuteAsync(CheckpointIdentity identity, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(identity);

        using var activity = ActivitySource.StartActivity("connector.execution.run", ActivityKind.Internal);
        activity?.SetTag("connector.name", identity.Connector);
        activity?.SetTag("connector.source", identity.Source);
        activity?.SetTag("connector.tenant", identity.Tenant);

        // Read-only boundary: this slice consumes checkpoint state only to derive the fetch window.
        // Checkpoint writes/mutations are intentionally deferred to W2-H2-T3.
        var checkpoint = await _checkpointStore.GetAsync(identity, ct);
        var now = _utcNow();
        var windowStart = checkpoint?.ProcessedAt ?? new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
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
            var result = await adapter.ExecuteAsync(request, ct);
            EmitTelemetry(activity, result);
            return result;
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

    private void EmitTelemetry(Activity? activity, ConnectorExecutionResult result, Exception? exception = null)
    {
        var correlationId = activity?.Id ?? Activity.Current?.Id ?? Guid.NewGuid().ToString("N");

        activity?.SetTag("correlation.id", correlationId);
        activity?.SetTag("execution.status", result.Status.ToString());
        activity?.SetTag("execution.item_count", result.ItemCount);

        if (result.Status == ConnectorExecutionStatus.Failure && !string.IsNullOrWhiteSpace(result.FailureReason))
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
    }
}
