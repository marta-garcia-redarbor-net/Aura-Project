using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Adapters.Connectors.Teams;

internal sealed partial class TeamsConnectorAdapter : IConnectorAdapter
{
    private readonly ILogger<TeamsConnectorAdapter> _logger;

    public TeamsConnectorAdapter(ILogger<TeamsConnectorAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public string ConnectorName => "teams";

    public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        const int stubItemCount = 1;
        Log.TeamsExecutionStubbed(_logger, request.Identity.Source, request.Identity.Tenant, request.WindowStart, request.WindowEnd, stubItemCount);

        return Task.FromResult(new ConnectorExecutionResult(
            request.Identity,
            stubItemCount,
            ConnectorExecutionStatus.Success,
            MaxProcessedAt: request.WindowEnd));
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 3201,
            Level = LogLevel.Information,
            Message = "Teams connector adapter executed (stub) for source {Source}, tenant {Tenant}, window {WindowStart} → {WindowEnd}, items {ItemCount}")]
        public static partial void TeamsExecutionStubbed(
            ILogger logger,
            string source,
            string tenant,
            DateTimeOffset windowStart,
            DateTimeOffset windowEnd,
            int itemCount);
    }
}
