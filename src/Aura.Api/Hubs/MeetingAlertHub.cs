using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Aura.Api.Hubs;

[Authorize]
public sealed class MeetingAlertHub : Hub
{
    private readonly IMeetingAlertStore _alertStore;

    public MeetingAlertHub(IMeetingAlertStore alertStore)
    {
        ArgumentNullException.ThrowIfNull(alertStore);
        _alertStore = alertStore;
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

    public async Task<bool> AcknowledgeAlert(string alertId, CancellationToken ct)
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        // Fetch upcoming alerts to find the one matching
        var now = DateTimeOffset.UtcNow;
        var alerts = await _alertStore.GetUpcomingAlertsAsync(now.AddHours(-1), now.AddHours(2), ct);

        var alert = alerts.FirstOrDefault(a =>
            a.EventId == alertId &&
            a.UserId == userId &&
            !a.HasBeenSent);

        if (alert is null)
        {
            return false;
        }

        await _alertStore.MarkSentAsync(alert, ct);
        return true;
    }
}
