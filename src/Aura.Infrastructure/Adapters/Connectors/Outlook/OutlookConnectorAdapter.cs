using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Adapters.Connectors.Outlook;

internal sealed partial class OutlookConnectorAdapter : IConnectorAdapter
{
    private static readonly Func<IReadOnlyList<OutlookEmailDto>> DefaultFixtureProvider = LoadDefaultFixtures;

    private readonly ILogger<OutlookConnectorAdapter> _logger;
    private readonly IWorkItemBuffer _buffer;
    private readonly OutlookWorkItemMapper _mapper;
    private readonly Func<IReadOnlyList<OutlookEmailDto>> _fixtureProvider;
    private readonly IMessageSourceProvider<OutlookEmailDto>? _sourceProvider;

    public OutlookConnectorAdapter(
        ILogger<OutlookConnectorAdapter> logger,
        IWorkItemBuffer buffer,
        OutlookWorkItemMapper mapper,
        Func<IReadOnlyList<OutlookEmailDto>>? fixtureProvider = null,
        IMessageSourceProvider<OutlookEmailDto>? sourceProvider = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(mapper);

        _logger = logger;
        _buffer = buffer;
        _mapper = mapper;
        _fixtureProvider = fixtureProvider ?? DefaultFixtureProvider;
        _sourceProvider = sourceProvider;
    }

    public string ConnectorName => "outlook";

    public async Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<OutlookEmailDto> payloads;

        if (_sourceProvider is not null)
        {
            payloads = await _sourceProvider.FetchAsync(request, ct);
        }
        else
        {
            payloads = _fixtureProvider();
        }

        var mappedCount = 0;
        var skippedCount = 0;

        foreach (var payload in payloads)
        {
            if (_mapper.TryMap(payload, out var workItem) && workItem is not null)
            {
                _buffer.Enqueue(workItem);
                mappedCount++;
                continue;
            }

            skippedCount++;
            Log.OutlookEmailSkipped(_logger, payload.ExternalId ?? "<missing>");
        }

        var status = skippedCount > 0 ? ConnectorExecutionStatus.PartialFailure : ConnectorExecutionStatus.Success;
        var failureReason = skippedCount > 0 ? $"Skipped {skippedCount} invalid Outlook payload(s)." : null;

        Log.OutlookExecutionMapped(_logger, request.Identity.Source, request.Identity.Tenant, request.WindowStart, request.WindowEnd, mappedCount, skippedCount);

        return new ConnectorExecutionResult(
            request.Identity,
            mappedCount,
            status,
            failureReason,
            MaxProcessedAt: request.WindowEnd);
    }

    private static IReadOnlyList<OutlookEmailDto> LoadDefaultFixtures()
        =>
        [
            new OutlookEmailDto
            {
                ExternalId = "outlook-mail-1001",
                Subject = "Urgent incident escalation",
                Importance = "High",
                SenderAddress = "ceo@aura.dev",
                BodyPreview = "production down immediate action required",
                ReceivedDateTime = DateTimeOffset.UtcNow,
                CorrelationId = "corr-outlook-1001",
                ConversationId = "conv-outlook-1001"
            },
            new OutlookEmailDto
            {
                ExternalId = null,
                Subject = "Missing id should be skipped",
                Importance = "Normal",
                SenderAddress = "manager@aura.dev",
                BodyPreview = "review today"
            },
            new OutlookEmailDto
            {
                ExternalId = "outlook-mail-1003",
                Subject = "Weekly status",
                Importance = null,
                SenderAddress = "unknown@aura.dev",
                BodyPreview = "regular note",
                ConversationId = "conv-outlook-1003"
            }
        ];

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 3203,
            Level = LogLevel.Information,
            Message = "Outlook connector adapter executed for source {Source}, tenant {Tenant}, window {WindowStart} → {WindowEnd}, mapped {MappedCount}, skipped {SkippedCount}")]
        public static partial void OutlookExecutionMapped(
            ILogger logger,
            string source,
            string tenant,
            DateTimeOffset windowStart,
            DateTimeOffset windowEnd,
            int mappedCount,
            int skippedCount);

        [LoggerMessage(
            EventId = 3204,
            Level = LogLevel.Warning,
            Message = "Outlook email skipped because required fields were missing. ExternalId={ExternalId}")]
        public static partial void OutlookEmailSkipped(
            ILogger logger,
            string externalId);
    }
}
