using Aura.Application.Ports;
using Aura.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Aura.Api.Adapters;

/// <summary>
/// Dispatches a dashboard refresh event via SignalR.
/// Targets a specific user group when provided, otherwise broadcasts to all clients.
/// </summary>
public sealed class SignalRDashboardRefreshDispatcher : IDashboardRefreshDispatcher
{
    private readonly IHubContext<AlertHub> _hubContext;

    public SignalRDashboardRefreshDispatcher(IHubContext<AlertHub> hubContext)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public Task DispatchAsync(string? userId, CancellationToken ct)
    {
        var payload = new
        {
            Timestamp = DateTimeOffset.UtcNow,
            Source = "workitem-change"
        };

        return string.IsNullOrWhiteSpace(userId)
            ? _hubContext.Clients.All.SendAsync("DashboardRefresh", payload, ct)
            : _hubContext.Clients.Group(userId).SendAsync("DashboardRefresh", payload, ct);
    }
}
