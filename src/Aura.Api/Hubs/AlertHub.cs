using Aura.Application.Ports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Aura.Api.Hubs;

/// <summary>
/// Unified SignalR hub for real-time alerts (meeting alerts and work item notifications).
/// Differentiated by event name: "MeetingAlert" and "UrgentWorkItem".
/// Group-by-userId pattern for targeted delivery.
/// </summary>
[Authorize]
public sealed class AlertHub : Hub
{
    private readonly IMeetingAlertStore _meetingAlertStore;
    private readonly INotificationOutboxStore _notificationOutboxStore;

    public AlertHub(
        IMeetingAlertStore meetingAlertStore,
        INotificationOutboxStore notificationOutboxStore)
    {
        ArgumentNullException.ThrowIfNull(meetingAlertStore);
        ArgumentNullException.ThrowIfNull(notificationOutboxStore);

        _meetingAlertStore = meetingAlertStore;
        _notificationOutboxStore = notificationOutboxStore;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Acknowledges a meeting alert by EventId.
    /// </summary>
    public async Task<bool> AcknowledgeAlert(string alertId, CancellationToken ct)
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        var alerts = await _meetingAlertStore.GetUpcomingAlertsAsync(now.AddHours(-1), now.AddHours(2), ct);

        var alert = alerts.FirstOrDefault(a =>
            a.EventId == alertId &&
            a.UserId == userId &&
            !a.HasBeenSent);

        if (alert is null)
        {
            return false;
        }

        await _meetingAlertStore.MarkSentAsync(alert, ct);
        return true;
    }

    /// <summary>
    /// Acknowledges a work item notification by outbox entry Id.
    /// </summary>
    public async Task<bool> AcknowledgeWorkItem(string notificationId, CancellationToken ct)
    {
        if (!Guid.TryParse(notificationId, out var id))
        {
            return false;
        }

        await _notificationOutboxStore.MarkDispatchedAsync(id, ct);
        return true;
    }
}
