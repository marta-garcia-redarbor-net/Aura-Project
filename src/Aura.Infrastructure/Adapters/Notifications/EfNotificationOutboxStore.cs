using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Adapters.Notifications;

/// <summary>
/// EF Core-backed outbox for cross-process notification entries.
/// Uses the <c>NotificationOutbox</c> table via <see cref="AuraDbContext"/>.
/// </summary>
internal sealed class EfNotificationOutboxStore : INotificationOutboxStore
{
    private readonly AuraDbContext _db;

    public EfNotificationOutboxStore(AuraDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task EnqueueAsync(NotificationOutboxEntry entry, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ct.ThrowIfCancellationRequested();

        _db.Notifications.Add(new Persistence.Notification
        {
            Id = entry.Id.ToString(),
            WorkItemId = entry.WorkItemId.ToString(),
            UserId = entry.UserId,
            SourceType = entry.SourceType,
            Title = entry.Title,
            Priority = entry.Priority,
            TriggerRule = entry.TriggerRule,
            CreatedAt = entry.CreatedAt.ToString("O"),
            DispatchedAt = null,
            Explanation = entry.Explanation,
            Decision = entry.Decision,
            TargetUserId = entry.TargetUserId,
            RuleResults = entry.RuleResults
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NotificationOutboxEntry>> GetPendingAsync(int limit, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entities = await _db.Notifications
            .AsNoTracking()
            .Where(e => e.DispatchedAt == null)
            .OrderByDescending(e => e.Priority)
            .ThenBy(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

        return entities.Select(MapToDomain).ToList();
    }

    public async Task MarkDispatchedAsync(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = await _db.Notifications
            .FirstOrDefaultAsync(e => e.Id == id.ToString(), ct);

        if (entity is not null)
        {
            entity.DispatchedAt = DateTimeOffset.UtcNow.ToString("O");
            await _db.SaveChangesAsync(ct);
        }
    }

    private static NotificationOutboxEntry MapToDomain(Persistence.Notification e)
    {
        return new NotificationOutboxEntry(
            id: Guid.Parse(e.Id),
            workItemId: Guid.Parse(e.WorkItemId),
            userId: e.UserId,
            sourceType: e.SourceType,
            title: e.Title,
            priority: e.Priority,
            triggerRule: e.TriggerRule,
            createdAt: DateTimeOffset.Parse(e.CreatedAt),
            dispatchedAt: e.DispatchedAt is not null ? DateTimeOffset.Parse(e.DispatchedAt) : null,
            explanation: e.Explanation,
            decision: e.Decision,
            targetUserId: e.TargetUserId,
            ruleResults: e.RuleResults);
    }
}
