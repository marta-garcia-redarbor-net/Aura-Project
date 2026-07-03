using Aura.Domain.WorkItems;

namespace Aura.Application.Ports;

/// <summary>
/// Cross-process outbox for notification entries.
/// Workers.exe enqueues entries; Api.exe reads pending entries and dispatches them via SignalR.
/// </summary>
public interface INotificationOutboxStore
{
    /// <summary>Enqueues a new notification outbox entry.</summary>
    Task EnqueueAsync(NotificationOutboxEntry entry, CancellationToken ct);

    /// <summary>
    /// Reads the top pending entries (up to <paramref name="limit"/>), ordered by priority descending then creation time ascending.
    /// </summary>
    Task<IReadOnlyList<NotificationOutboxEntry>> GetPendingAsync(int limit, CancellationToken ct);

    /// <summary>Marks an entry as dispatched by setting its DispatchedAt timestamp.</summary>
    Task MarkDispatchedAsync(Guid id, CancellationToken ct);
}
