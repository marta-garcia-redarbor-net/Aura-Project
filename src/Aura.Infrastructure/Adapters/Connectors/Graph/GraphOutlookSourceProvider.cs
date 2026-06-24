using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Outlook;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// Fetches Outlook emails from Microsoft Graph API via the /me/messages endpoint.
/// Maps Graph response to <see cref="OutlookEmailDto"/> for the connector adapter pipeline.
/// </summary>
internal sealed partial class GraphOutlookSourceProvider : IMessageSourceProvider<OutlookEmailDto>
{
    private readonly IGraphClientFactory _clientFactory;
    private readonly ILogger<GraphOutlookSourceProvider> _logger;

    public GraphOutlookSourceProvider(IGraphClientFactory clientFactory, ILogger<GraphOutlookSourceProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OutlookEmailDto>> FetchAsync(ConnectorExecutionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var client = await _clientFactory.CreateClientAsync(ct);

        var messages = await client.Me.Messages.GetAsync(requestConfig =>
        {
            requestConfig.QueryParameters.Top = 50;
            requestConfig.QueryParameters.Orderby = ["receivedDateTime desc"];
            requestConfig.QueryParameters.Select = ["id", "subject", "importance", "sender", "bodyPreview", "webLink", "receivedDateTime", "conversationId"];
        }, ct);

        if (messages?.Value is null || messages.Value.Count == 0)
        {
            Log.NoOutlookMessages(_logger);
            return [];
        }

        var results = new List<OutlookEmailDto>(messages.Value.Count);
        foreach (var msg in messages.Value)
        {
            var dto = new OutlookEmailDto
            {
                ExternalId = msg.Id,
                Subject = msg.Subject,
                Importance = msg.Importance?.ToString(),
                SenderAddress = msg.Sender?.EmailAddress?.Address,
                BodyPreview = msg.BodyPreview?.Length > 200
                    ? msg.BodyPreview[..200]
                    : msg.BodyPreview,
                ReceivedDateTime = msg.ReceivedDateTime,
                ConversationId = msg.ConversationId,
                WebLink = msg.WebLink
            };

            results.Add(dto);
        }

        Log.OutlookFetched(_logger, results.Count);
        return results;
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 3303, Level = LogLevel.Information,
            Message = "GraphOutlookSourceProvider fetched {Count} emails")]
        public static partial void OutlookFetched(ILogger logger, int count);

        [LoggerMessage(EventId = 3304, Level = LogLevel.Information,
            Message = "GraphOutlookSourceProvider returned zero emails from Graph API")]
        public static partial void NoOutlookMessages(ILogger logger);
    }
}
