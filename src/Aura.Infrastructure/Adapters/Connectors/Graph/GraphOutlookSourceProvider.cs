using System.Diagnostics.Metrics;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Outlook;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// Fetches Outlook emails from Microsoft Graph API via the /me/messages endpoint.
/// Maps Graph response to <see cref="OutlookEmailDto"/> for the connector adapter pipeline.
/// </summary>
internal sealed partial class GraphOutlookSourceProvider : IMessageSourceProvider<OutlookEmailDto>
{
    private static readonly Meter s_meter = new("Aura.Infrastructure.GraphConnector");
    private static readonly Counter<long> s_tokenAcquired = s_meter.CreateCounter<long>("graph.token.acquired");
    private static readonly Counter<long> s_tokenExpired = s_meter.CreateCounter<long>("graph.token.expired");
    private static readonly Counter<long> s_graphHttpError = s_meter.CreateCounter<long>("graph.http.error");

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

        GraphServiceClient client;
        try
        {
            client = await _clientFactory.CreateClientAsync(request.Identity.UserOid ?? "default", ct);
            s_tokenAcquired.Add(1, new KeyValuePair<string, object?>("connector", "outlook"));
        }
        catch (Microsoft.Identity.Client.MsalUiRequiredException)
        {
            Log.TokenExpired(_logger, request.Identity.UserOid ?? "unknown");
            s_tokenExpired.Add(1,
                new KeyValuePair<string, object?>("connector", "outlook"),
                new KeyValuePair<string, object?>("oid", request.Identity.UserOid ?? "unknown"));
            throw;
        }

        MessageCollectionResponse messages;
        try
        {
            messages = await client.Me.MailFolders["inbox"].Messages.GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.Top = 50;
                requestConfig.QueryParameters.Filter = "isRead eq false";
                requestConfig.QueryParameters.Orderby = ["receivedDateTime desc"];
                requestConfig.QueryParameters.Select = ["id", "subject", "importance", "sender", "bodyPreview", "webLink", "receivedDateTime", "conversationId", "isRead"];
            }, ct);
        }
        catch (ODataError ex) when (ex.ResponseStatusCode is >= 400 and < 600)
        {
            Log.GraphHttpError(_logger, ex.ResponseStatusCode, "me/mailFolders/inbox/messages");
            s_graphHttpError.Add(1,
                new KeyValuePair<string, object?>("connector", "outlook"),
                new KeyValuePair<string, object?>("status_code", ex.ResponseStatusCode),
                new KeyValuePair<string, object?>("endpoint", "me/mailFolders/inbox/messages"));
            throw;
        }

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
                UserOid = request.Identity.UserOid,
                WebLink = msg.WebLink,
                IsRead = msg.IsRead ?? false
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

        [LoggerMessage(EventId = 3307, Level = LogLevel.Warning,
            Message = "GraphOutlookSourceProvider token expired for oid={Oid}. Re-authentication required.")]
        public static partial void TokenExpired(ILogger logger, string oid);

        [LoggerMessage(EventId = 3308, Level = LogLevel.Warning,
            Message = "GraphOutlookSourceProvider HTTP {StatusCode} from {Endpoint}")]
        public static partial void GraphHttpError(ILogger logger, int statusCode, string endpoint);
    }
}
