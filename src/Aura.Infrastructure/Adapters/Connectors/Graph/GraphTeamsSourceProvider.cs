using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Teams;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// Fetches Teams chat messages from Microsoft Graph API via the /me/chats/messages endpoint.
/// Maps Graph response to <see cref="TeamsMessageDto"/> for the connector adapter pipeline.
/// </summary>
internal sealed partial class GraphTeamsSourceProvider : IMessageSourceProvider<TeamsMessageDto>
{
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

        var client = await _clientFactory.CreateClientAsync(ct);

        var messages = await client.Me.Chats.GetAsync(requestConfig =>
        {
            requestConfig.QueryParameters.Top = 50;
        }, ct);

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
                Sender = chat.LastMessagePreview?.From?.User?.DisplayName,
                BodyPreview = chat.LastMessagePreview?.Body?.Content?.Length > 200
                    ? chat.LastMessagePreview.Body.Content[..200]
                    : chat.LastMessagePreview?.Body?.Content,
                CapturedAtUtc = chat.LastUpdatedDateTime ?? DateTimeOffset.UtcNow
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
    }
}
