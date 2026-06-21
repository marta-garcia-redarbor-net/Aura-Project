using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Adapters.Connectors.Teams;

internal sealed partial class TeamsConnectorAdapter : IConnectorAdapter
{
    private static readonly Func<IReadOnlyList<TeamsMessageDto>> DefaultFixtureProvider = LoadDefaultFixtures;

    private readonly ILogger<TeamsConnectorAdapter> _logger;
    private readonly IWorkItemBuffer _buffer;
    private readonly TeamsWorkItemMapper _mapper;
    private readonly Func<IReadOnlyList<TeamsMessageDto>> _fixtureProvider;

    public TeamsConnectorAdapter(
        ILogger<TeamsConnectorAdapter> logger,
        IWorkItemBuffer buffer,
        TeamsWorkItemMapper mapper,
        Func<IReadOnlyList<TeamsMessageDto>>? fixtureProvider = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(mapper);
        _logger = logger;
        _buffer = buffer;
        _mapper = mapper;
        _fixtureProvider = fixtureProvider ?? DefaultFixtureProvider;
    }

    public string ConnectorName => "teams";

    public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var payloads = _fixtureProvider();

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
            Log.TeamsMessageSkipped(_logger, payload.ExternalId ?? "<missing>");
        }

        var status = skippedCount > 0 ? ConnectorExecutionStatus.PartialFailure : ConnectorExecutionStatus.Success;
        var failureReason = skippedCount > 0 ? $"Skipped {skippedCount} invalid Teams payload(s)." : null;

        Log.TeamsExecutionMapped(_logger, request.Identity.Source, request.Identity.Tenant, request.WindowStart, request.WindowEnd, mappedCount, skippedCount);

        return Task.FromResult(new ConnectorExecutionResult(
            request.Identity,
            mappedCount,
            status,
            failureReason,
            MaxProcessedAt: request.WindowEnd));
    }

    private static IReadOnlyList<TeamsMessageDto> LoadDefaultFixtures()
        =>
        [
            new TeamsMessageDto
            {
                ExternalId = "teams-msg-1001",
                Title = "Escalate production incident",
                Source = "messages",
                Priority = "high",
                TeamId = "team-a",
                ChannelId = "channel-ops",
                MessageUrl = "https://teams/messages/1001",
                CorrelationId = "corr-1001",
                CapturedAtUtc = DateTimeOffset.UtcNow
            },
            new TeamsMessageDto
            {
                ExternalId = null,
                Title = "Missing id should be skipped",
                Source = "messages",
                Priority = "low",
                TeamId = "team-a",
                ChannelId = "channel-ops"
            },
            new TeamsMessageDto
            {
                ExternalId = "teams-msg-1003",
                Title = "Needs triage",
                Source = "messages",
                Priority = "unknown-priority",
                TeamId = "team-b",
                ChannelId = "channel-triage"
            }
        ];

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 3201,
            Level = LogLevel.Information,
            Message = "Teams connector adapter executed for source {Source}, tenant {Tenant}, window {WindowStart} → {WindowEnd}, mapped {MappedCount}, skipped {SkippedCount}")]
        public static partial void TeamsExecutionMapped(
            ILogger logger,
            string source,
            string tenant,
            DateTimeOffset windowStart,
            DateTimeOffset windowEnd,
            int mappedCount,
            int skippedCount);

        [LoggerMessage(
            EventId = 3202,
            Level = LogLevel.Warning,
            Message = "Teams message skipped because required fields were missing. ExternalId={ExternalId}")]
        public static partial void TeamsMessageSkipped(
            ILogger logger,
            string externalId);
    }
}
