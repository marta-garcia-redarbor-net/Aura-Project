using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Microsoft.Extensions.Logging;

namespace Aura.Infrastructure.Adapters.Calendar;

internal sealed partial class LoggingMeetingAlertDispatcher : IMeetingAlertDispatcher
{
    private readonly ILogger<LoggingMeetingAlertDispatcher> _logger;

    public LoggingMeetingAlertDispatcher(ILogger<LoggingMeetingAlertDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public Task DispatchAsync(MeetingAlert alert, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(alert);

        Log.AlertDispatched(_logger, alert.EventId, alert.Title, alert.Trigger.ToString(), alert.StartsAtUtc);
        return Task.CompletedTask;
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 4502, Level = LogLevel.Information,
            Message = "Meeting alert dispatched (logging): EventId={EventId}, Title={Title}, Trigger={Trigger}, StartsAtUtc={StartsAtUtc}")]
        public static partial void AlertDispatched(ILogger logger, string eventId, string title, string trigger, DateTimeOffset startsAtUtc);
    }
}
