using Aura.Application.Ports;
using Aura.Api.Hubs;
using Aura.Domain.Calendar;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Adapters;

internal sealed partial class SignalRMeetingAlertDispatcher : IMeetingAlertDispatcher
{
    private readonly IHubContext<MeetingAlertHub> _hubContext;
    private readonly ILogger<SignalRMeetingAlertDispatcher> _logger;

    public SignalRMeetingAlertDispatcher(
        IHubContext<MeetingAlertHub> hubContext,
        ILogger<SignalRMeetingAlertDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(hubContext);
        ArgumentNullException.ThrowIfNull(logger);

        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task DispatchAsync(MeetingAlert alert, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(alert);

        await _hubContext.Clients.Group(alert.UserId).SendAsync(
            "MeetingAlert",
            new
            {
                alert.EventId,
                alert.Title,
                Trigger = alert.Trigger.ToString(),
                alert.StartsAtUtc,
                alert.JoinUrl
            },
            ct);

        Log.AlertDispatched(_logger, alert.EventId, alert.Trigger.ToString());
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 4501, Level = LogLevel.Information,
            Message = "Meeting alert dispatched for EventId={EventId}, Trigger={Trigger}")]
        public static partial void AlertDispatched(ILogger logger, string eventId, string trigger);
    }
}
