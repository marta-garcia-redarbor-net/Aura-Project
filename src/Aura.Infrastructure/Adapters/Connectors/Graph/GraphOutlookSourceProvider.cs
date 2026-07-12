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

        var userOid = request.Identity.UserOid ?? "default";
        Log.OutlookFetchStarted(_logger, userOid, "outlook", "me/mailFolders/inbox/messages");

        GraphServiceClient client;
        try
        {
            client = await _clientFactory.CreateClientAsync(userOid, ct);
            s_tokenAcquired.Add(1, new KeyValuePair<string, object?>("connector", "outlook"));
        }
        catch (Microsoft.Identity.Client.MsalUiRequiredException)
        {
            Log.TokenExpired(_logger, request.Identity.UserOid ?? "unknown", "outlook");
            s_tokenExpired.Add(1,
                new KeyValuePair<string, object?>("connector", "outlook"),
                new KeyValuePair<string, object?>("oid", request.Identity.UserOid ?? "unknown"));
            throw;
        }
        catch (Exception ex) when (TryResolveStatusCode(ex, out var statusCode) && statusCode is >= 400 and < 600)
        {
            if (statusCode >= 500)
            {
                Log.GraphHttpServerError(_logger, statusCode, "me/mailFolders/inbox/messages", "outlook");
            }
            else
            {
                Log.GraphHttpClientError(_logger, statusCode, "me/mailFolders/inbox/messages", "outlook");
            }

            s_graphHttpError.Add(1,
                new KeyValuePair<string, object?>("connector", "outlook"),
                new KeyValuePair<string, object?>("status_code", statusCode),
                new KeyValuePair<string, object?>("endpoint", "me/mailFolders/inbox/messages"));
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
        catch (Exception ex) when (TryResolveStatusCode(ex, out var statusCode) && statusCode is >= 400 and < 600)
        {
            if (statusCode >= 500)
            {
                Log.GraphHttpServerError(_logger, statusCode, "me/mailFolders/inbox/messages", "outlook");
            }
            else
            {
                Log.GraphHttpClientError(_logger, statusCode, "me/mailFolders/inbox/messages", "outlook");
            }

            s_graphHttpError.Add(1,
                new KeyValuePair<string, object?>("connector", "outlook"),
                new KeyValuePair<string, object?>("status_code", statusCode),
                new KeyValuePair<string, object?>("endpoint", "me/mailFolders/inbox/messages"));
            throw;
        }

        if (messages?.Value is null || messages.Value.Count == 0)
        {
            Log.NoOutlookMessages(_logger, userOid, "outlook");
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

        Log.OutlookFetched(_logger, results.Count, userOid, "outlook");
        return results;
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 3303, Level = LogLevel.Information,
            Message = "GraphOutlookSourceProvider fetched {Count} emails for oid={Oid} connector={Connector}")]
        public static partial void OutlookFetched(ILogger logger, int count, string oid, string connector);

        [LoggerMessage(EventId = 3304, Level = LogLevel.Information,
            Message = "GraphOutlookSourceProvider returned zero emails from Graph API for oid={Oid} connector={Connector}")]
        public static partial void NoOutlookMessages(ILogger logger, string oid, string connector);

        [LoggerMessage(EventId = 3305, Level = LogLevel.Information,
            Message = "GraphOutlookSourceProvider fetch started for oid={Oid} connector={Connector} endpoint={Endpoint}")]
        public static partial void OutlookFetchStarted(ILogger logger, string oid, string connector, string endpoint);

        [LoggerMessage(EventId = 3307, Level = LogLevel.Warning,
            Message = "GraphOutlookSourceProvider token expired for oid={Oid} connector={Connector}. Re-authentication required.")]
        public static partial void TokenExpired(ILogger logger, string oid, string connector);

        [LoggerMessage(EventId = 3308, Level = LogLevel.Warning,
            Message = "GraphOutlookSourceProvider HTTP {StatusCode} from {Endpoint} connector={Connector}")]
        public static partial void GraphHttpClientError(ILogger logger, int statusCode, string endpoint, string connector);

        [LoggerMessage(EventId = 3309, Level = LogLevel.Error,
            Message = "GraphOutlookSourceProvider HTTP {StatusCode} from {Endpoint} connector={Connector}")]
        public static partial void GraphHttpServerError(ILogger logger, int statusCode, string endpoint, string connector);
    }

    private static bool TryResolveStatusCode(Exception exception, out int statusCode)
    {
        if (exception is ODataError odata && odata.ResponseStatusCode > 0)
        {
            statusCode = odata.ResponseStatusCode;
            return true;
        }

        var statusProperty = exception.GetType().GetProperty("ResponseStatusCode");
        if (statusProperty is not null)
        {
            var value = statusProperty.GetValue(exception);
            if (value is int intValue)
            {
                statusCode = intValue;
                return true;
            }
        }

        var statusCodeProperty = exception.GetType().GetProperty("StatusCode");
        if (statusCodeProperty is not null)
        {
            var value = statusCodeProperty.GetValue(exception);
            if (value is int intValue)
            {
                statusCode = intValue;
                return true;
            }

            if (value is System.Net.HttpStatusCode enumValue)
            {
                statusCode = (int)enumValue;
                return true;
            }
        }

        statusCode = 0;
        return false;
    }
}
