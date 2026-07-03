using Aura.Application.Models;
using Aura.Domain.WorkItems;

namespace Aura.Application.Ports;

/// <summary>
/// Dispatches a notification to the user via the appropriate channel (e.g., SignalR).
/// </summary>
public interface IWorkItemNotificationDispatcher
{
    /// <summary>
    /// Dispatches the notification to the user associated with the outbox entry.
    /// </summary>
    Task DispatchAsync(NotificationOutboxEntry entry, InterruptionVerdict verdict, CancellationToken ct);
}
