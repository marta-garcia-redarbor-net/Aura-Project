using System.Diagnostics.Metrics;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace Aura.Infrastructure.Adapters.Connectors.Calendar;

internal sealed partial class GraphCalendarEventProvider : IMessageSourceProvider<CalendarEventDto>
{
    private static readonly Meter s_meter = new("Aura.Infrastructure.GraphConnector");
    private static readonly Counter<long> s_tokenAcquired = s_meter.CreateCounter<long>("graph.token.acquired");
    private static readonly Counter<long> s_tokenExpired = s_meter.CreateCounter<long>("graph.token.expired");
    private static readonly Counter<long> s_graphHttpError = s_meter.CreateCounter<long>("graph.http.error");

    private readonly IGraphClientFactory _clientFactory;
    private readonly ILogger<GraphCalendarEventProvider> _logger;

    public GraphCalendarEventProvider(IGraphClientFactory clientFactory, ILogger<GraphCalendarEventProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CalendarEventDto>> FetchAsync(ConnectorExecutionRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        GraphServiceClient client;
        try
        {
            client = await _clientFactory.CreateClientAsync(request.Identity.UserOid ?? "default", ct);
            s_tokenAcquired.Add(1, new KeyValuePair<string, object?>("connector", "calendar"));
        }
        catch (Microsoft.Identity.Client.MsalUiRequiredException)
        {
            Log.TokenExpired(_logger, request.Identity.UserOid ?? "unknown");
            s_tokenExpired.Add(1,
                new KeyValuePair<string, object?>("connector", "calendar"),
                new KeyValuePair<string, object?>("oid", request.Identity.UserOid ?? "unknown"));
            throw;
        }

        EventCollectionResponse events;
        try
        {
            events = await client.Me.CalendarView.GetAsync(requestConfig =>
            {
                requestConfig.QueryParameters.StartDateTime = request.WindowStart.ToString("o");
                requestConfig.QueryParameters.EndDateTime = request.WindowEnd.ToString("o");
                requestConfig.QueryParameters.Top = 50;
            }, ct);
        }
        catch (ODataError ex) when (ex.ResponseStatusCode is >= 400 and < 600)
        {
            Log.GraphHttpError(_logger, ex.ResponseStatusCode, "me/calendarView");
            s_graphHttpError.Add(1,
                new KeyValuePair<string, object?>("connector", "calendar"),
                new KeyValuePair<string, object?>("status_code", ex.ResponseStatusCode),
                new KeyValuePair<string, object?>("endpoint", "me/calendarView"));
            throw;
        }

        if (events?.Value is null || events.Value.Count == 0)
        {
            Log.NoCalendarEvents(_logger);
            return [];
        }

        var results = new List<CalendarEventDto>(events.Value.Count);
        foreach (var graphEvent in events.Value)
        {
            var dto = new CalendarEventDto
            {
                ExternalId = graphEvent.Id,
                Subject = graphEvent.Subject,
                Start = ParseDateTimeOffset(graphEvent.Start),
                End = ParseDateTimeOffset(graphEvent.End),
                IsOnlineMeeting = graphEvent.IsOnlineMeeting ?? false,
                JoinUrl = graphEvent.OnlineMeeting?.JoinUrl,
                OrganizerName = graphEvent.Organizer?.EmailAddress?.Name,
                OrganizerAddress = graphEvent.Organizer?.EmailAddress?.Address,
                LocationDisplayName = graphEvent.Location?.DisplayName,
                IsCancelled = graphEvent.IsCancelled ?? false,
                OriginalTimeZone = graphEvent.OriginalStartTimeZone
            };

            results.Add(dto);
        }

        Log.CalendarEventsFetched(_logger, results.Count);
        return results;
    }

    private static DateTimeOffset? ParseDateTimeOffset(Microsoft.Graph.Models.DateTimeTimeZone? dateTimeTimeZone)
    {
        if (dateTimeTimeZone is null || string.IsNullOrEmpty(dateTimeTimeZone.DateTime))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(dateTimeTimeZone.DateTime, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out var dto))
        {
            return dto;
        }

        return null;
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 3401, Level = LogLevel.Information,
            Message = "GraphCalendarEventProvider fetched {Count} calendar events")]
        public static partial void CalendarEventsFetched(ILogger logger, int count);

        [LoggerMessage(EventId = 3402, Level = LogLevel.Information,
            Message = "GraphCalendarEventProvider returned zero calendar events from Graph API")]
        public static partial void NoCalendarEvents(ILogger logger);

        [LoggerMessage(EventId = 3403, Level = LogLevel.Warning,
            Message = "GraphCalendarEventProvider token expired for oid={Oid}. Re-authentication required.")]
        public static partial void TokenExpired(ILogger logger, string oid);

        [LoggerMessage(EventId = 3404, Level = LogLevel.Warning,
            Message = "GraphCalendarEventProvider HTTP {StatusCode} from {Endpoint}")]
        public static partial void GraphHttpError(ILogger logger, int statusCode, string endpoint);
    }
}
