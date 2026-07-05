using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Api.Hubs;
using Aura.Domain.WorkItems;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Adapters;

/// <summary>
/// Dispatches work item notifications to the user via SignalR on the unified <see cref="AlertHub"/>.
/// Sends "UrgentWorkItem" event to the user's group.
/// </summary>
public sealed partial class SignalRWorkItemNotificationDispatcher : IWorkItemNotificationDispatcher
{
    private readonly IHubContext<AlertHub> _hubContext;
    private readonly ILogger<SignalRWorkItemNotificationDispatcher> _logger;

    public SignalRWorkItemNotificationDispatcher(
        IHubContext<AlertHub> hubContext,
        ILogger<SignalRWorkItemNotificationDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(hubContext);
        ArgumentNullException.ThrowIfNull(logger);

        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task DispatchAsync(NotificationOutboxEntry entry, InterruptionVerdict verdict, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(verdict);

        ct.ThrowIfCancellationRequested();

        await _hubContext.Clients.Group(entry.UserId).SendAsync(
            "UrgentWorkItem",
            new
            {
                Id = entry.Id.ToString(),
                entry.Title,
                entry.SourceType,
                entry.Priority,
                entry.TriggerRule,
                Reason = verdict.TriggerRule ?? "Policy evaluation",
                Explanation = verdict.Explanation,
                Decision = verdict.Decision.ToString(),
                TargetUserId = verdict.TargetUserId,
                RuleResults = entry.RuleResults
            },
            ct);

        Log.UrgentWorkItemDispatched(_logger, entry.Id, entry.Title, entry.Priority);
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 4601, Level = LogLevel.Information,
            Message = "Urgent work item notification dispatched: Id={NotificationId}, Title={Title}, Priority={Priority}")]
        public static partial void UrgentWorkItemDispatched(ILogger logger, Guid notificationId, string title, double priority);
    }
}
