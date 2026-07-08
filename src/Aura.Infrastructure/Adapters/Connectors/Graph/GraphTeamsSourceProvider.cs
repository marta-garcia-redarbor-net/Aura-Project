using System.Diagnostics.Metrics;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Teams;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// Fetches Teams chat messages from Microsoft Graph API via the /me/chats/messages endpoint.
/// Maps Graph response to <see cref="TeamsMessageDto"/> for the connector adapter pipeline.
/// </summary>
internal sealed partial class GraphTeamsSourceProvider : IMessageSourceProvider<TeamsMessageDto>
{
    private static readonly Meter s_meter = new("Aura.Infrastructure.GraphConnector");
    private static readonly Counter<long> s_tokenAcquired = s_meter.CreateCounter<long>("graph.token.acquired");
    private static readonly Counter<long> s_tokenExpired = s_meter.CreateCounter<long>("graph.token.expired");
    private static readonly Counter<long> s_graphHttpError = s_meter.CreateCounter<long>("graph.http.error");

    private readonly IGraphClientFactory _clientFactory;
    private readonly ILogger<GraphTeamsSourceProvider> _logger;

    public GraphTeamsSourceProvider(IGraphClientFactory clientFactory, ILogger<GraphTeamsSourceProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TeamsMessageDto>> FetchAsync(ConnectorExecutionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        GraphServiceClient client;
        try
        {
            client = await _clientFactory.CreateClientAsync(request.Identity.UserOid ?? "default", ct);
            s_tokenAcquired.Add(1, new KeyValuePair<string, object?>("connector", "teams"));
        }
        catch (Microsoft.Identity.Client.MsalUiRequiredException)
        {
            Log.TokenExpired(_logger, request.Identity.UserOid ?? "unknown", "teams");
            s_tokenExpired.Add(1,
                new KeyValuePair<string, object?>("connector", "teams"),
                new KeyValuePair<string, object?>("oid", request.Identity.UserOid ?? "unknown"));
            throw;
        }
        catch (Exception ex) when (TryResolveStatusCode(ex, out var statusCode) && statusCode is >= 400 and < 600)
        {
            if (statusCode >= 500)
            {
                Log.GraphHttpServerError(_logger, statusCode, "me/chats", "teams");
            }
            else
            {
                Log.GraphHttpClientError(_logger, statusCode, "me/chats", "teams");
            }

            s_graphHttpError.Add(1,
                new KeyValuePair<string, object?>("connector", "teams"),
                new KeyValuePair<string, object?>("status_code", statusCode),
                new KeyValuePair<string, object?>("endpoint", "me/chats"));
            throw;
        }

        ChatCollectionResponse messages;
        try
        {
            messages = await client.Me.Chats.GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.Top = 50;
            }, ct);
        }
        catch (Exception ex) when (TryResolveStatusCode(ex, out var statusCode) && statusCode is >= 400 and < 600)
        {
            if (statusCode >= 500)
            {
                Log.GraphHttpServerError(_logger, statusCode, "me/chats", "teams");
            }
            else
            {
                Log.GraphHttpClientError(_logger, statusCode, "me/chats", "teams");
            }

            s_graphHttpError.Add(1,
                new KeyValuePair<string, object?>("connector", "teams"),
                new KeyValuePair<string, object?>("status_code", statusCode),
                new KeyValuePair<string, object?>("endpoint", "me/chats"));
            throw;
        }

        if (messages?.Value is null || messages.Value.Count == 0)
        {
            Log.NoTeamsMessages(_logger);
            return [];
        }

        var results = new List<TeamsMessageDto>(messages.Value.Count);
        foreach (var chat in messages.Value)
        {
            var dto = new TeamsMessageDto
            {
                ExternalId = chat.Id,
                Title = chat.Topic ?? $"Teams chat {chat.Id}",
                Source = "chats",
                TeamId = null,
                ChannelId = null,
                MessageUrl = chat.WebUrl,
                WebUrl = chat.WebUrl,
                UserOid = request.Identity.UserOid,
                Sender = chat.LastMessagePreview?.From?.User?.DisplayName,
                BodyPreview = chat.LastMessagePreview?.Body?.Content?.Length > 200
                    ? chat.LastMessagePreview.Body.Content[..200]
                    : chat.LastMessagePreview?.Body?.Content,
                CapturedAtUtc = chat.LastUpdatedDateTime ?? DateTimeOffset.UtcNow,
                LastMessageReadAt = chat.Viewpoint?.LastMessageReadDateTime,
                LastMessageAt = chat.LastUpdatedDateTime,
                UnreadCount = 0
            };

            results.Add(dto);
        }

        Log.TeamsFetched(_logger, results.Count);
        return results;
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 3301, Level = LogLevel.Information,
            Message = "GraphTeamsSourceProvider fetched {Count} messages")]
        public static partial void TeamsFetched(ILogger logger, int count);

        [LoggerMessage(EventId = 3302, Level = LogLevel.Information,
            Message = "GraphTeamsSourceProvider returned zero messages from Graph API")]
        public static partial void NoTeamsMessages(ILogger logger);

        [LoggerMessage(EventId = 3305, Level = LogLevel.Warning,
            Message = "GraphTeamsSourceProvider token expired for oid={Oid} connector={Connector}. Re-authentication required.")]
        public static partial void TokenExpired(ILogger logger, string oid, string connector);

        [LoggerMessage(EventId = 3306, Level = LogLevel.Warning,
            Message = "GraphTeamsSourceProvider HTTP {StatusCode} from {Endpoint} connector={Connector}")]
        public static partial void GraphHttpClientError(ILogger logger, int statusCode, string endpoint, string connector);

        [LoggerMessage(EventId = 3309, Level = LogLevel.Error,
            Message = "GraphTeamsSourceProvider HTTP {StatusCode} from {Endpoint} connector={Connector}")]
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
