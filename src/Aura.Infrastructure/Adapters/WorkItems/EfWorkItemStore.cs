using System.Text.Json;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Adapters.WorkItems;

/// <summary>
/// EF Core-backed work item store with idempotent upsert on ExternalId.
/// Also provides read capability via <see cref="IWorkItemReader"/>.
/// Uses the <c>WorkItems</c> table via <see cref="AuraDbContext"/>.
/// </summary>
internal sealed class EfWorkItemStore : IWorkItemStore, IWorkItemReader
{
    private readonly AuraDbContext _db;

    public EfWorkItemStore(AuraDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<WorkItemPersistenceResult> SaveAsync(WorkItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ct.ThrowIfCancellationRequested();

        var existing = await _db.WorkItems
            .FirstOrDefaultAsync(e => e.ExternalId == item.ExternalId, ct);

        if (existing is not null)
        {
            existing.Title = item.Title;
            existing.Source = item.Source;
            existing.SourceType = item.SourceType.ToString();
            existing.Priority = item.Priority.ToString();
            existing.MetadataJson = JsonSerializer.Serialize(item.Metadata);
            existing.CorrelationId = item.CorrelationId;
            existing.CapturedAtUtc = item.CapturedAtUtc.ToString("O");
            existing.SchemaVersion = item.SchemaVersion;
            existing.Status = item.Status.ToString();
            existing.FaultReason = item.FaultReason;
            existing.PriorityScore = item.PriorityScore;
            existing.OwnerUserId = item.OwnerUserId;
            existing.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");
        }
        else
        {
            _db.WorkItems.Add(new Persistence.WorkItemEntity
            {
                Id = item.Id.ToString(),
                ExternalId = item.ExternalId,
                Title = item.Title,
                Source = item.Source,
                SourceType = item.SourceType.ToString(),
                Priority = item.Priority.ToString(),
                MetadataJson = JsonSerializer.Serialize(item.Metadata),
                CorrelationId = item.CorrelationId,
                CapturedAtUtc = item.CapturedAtUtc.ToString("O"),
                SchemaVersion = item.SchemaVersion,
                Status = item.Status.ToString(),
                CreatedAt = item.CreatedAt.ToString("O"),
                FaultReason = item.FaultReason,
                PriorityScore = item.PriorityScore,
                OwnerUserId = item.OwnerUserId
            });
        }

        await _db.SaveChangesAsync(ct);
        return WorkItemPersistenceResult.Success();
    }

    public async Task<WorkItem?> FindByExternalIdAsync(string externalId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(externalId);
        ct.ThrowIfCancellationRequested();

        var entity = await _db.WorkItems
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ExternalId == externalId, ct);

        if (entity is null)
            return null;

        return MapToDomain(entity);
    }

    public async Task<IReadOnlySet<string>> GetPendingExternalIdsAsync(WorkItemSourceType source, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var ids = await _db.WorkItems
            .AsNoTracking()
            .Where(e => e.Status == "Pending" && e.SourceType == source.ToString())
            .Select(e => e.ExternalId)
            .ToListAsync(ct);

        return new HashSet<string>(ids, StringComparer.Ordinal);
    }

    public async Task MarkCompletedAsync(IReadOnlySet<string> externalIds, WorkItemSourceType source, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(externalIds);
        ct.ThrowIfCancellationRequested();

        if (externalIds.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow.ToString("O");

        var entities = await _db.WorkItems
            .Where(e => externalIds.Contains(e.ExternalId)
                     && e.Status == "Pending"
                     && e.SourceType == source.ToString())
            .ToListAsync(ct);

        foreach (var entity in entities)
        {
            entity.Status = "Completed";
            entity.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<WorkItem>> ReadBySourceAsync(
        WorkItemSourceType sourceType,
        WorkItemStatus? statusFilter,
        string? ownerUserId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _db.WorkItems
            .AsNoTracking()
            .Where(e => e.SourceType == sourceType.ToString());

        if (statusFilter.HasValue)
        {
            var statusStr = statusFilter.Value.ToString();
            query = query.Where(e => e.Status == statusStr);
        }

        if (ownerUserId is not null)
        {
            query = query.Where(e => e.OwnerUserId == null || e.OwnerUserId == ownerUserId);
        }

        var entities = await query
            .OrderByDescending(e => e.PriorityScore ?? 0)
            .ThenByDescending(e => e.CapturedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToList();
    }

    public Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(MorningSummaryQuery query, CancellationToken cancellationToken)
        => ReadForWindowAsync(query, null, cancellationToken);

    public async Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
        MorningSummaryQuery query, WorkItemStatus? statusFilter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var fromUtc = query.FromUtc.ToString("O");
        var toUtc = query.ToUtc.ToString("O");

        var dbQuery = _db.WorkItems
            .AsNoTracking()
            .Where(e => string.Compare(e.CapturedAtUtc, fromUtc) >= 0 && string.Compare(e.CapturedAtUtc, toUtc) <= 0);

        // Filter by OwnerUserId: only items owned by the query user, or items visible to all
        if (!string.IsNullOrEmpty(query.UserId))
        {
            dbQuery = dbQuery.Where(e => e.OwnerUserId == null || e.OwnerUserId == query.UserId);
        }

        if (statusFilter.HasValue)
        {
            var statusStr = statusFilter.Value.ToString();
            dbQuery = dbQuery.Where(e => e.Status == statusStr);
        }

        var entities = await dbQuery
            .OrderByDescending(e => e.CapturedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToList();
    }

    public async Task<IReadOnlyList<WorkItem>> ReadForWindowAsync(
        WorkItemSourceType sourceType, MorningSummaryQuery query, WorkItemStatus? statusFilter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        var fromUtc = query.FromUtc.ToString("O");
        var toUtc = query.ToUtc.ToString("O");

        var dbQuery = _db.WorkItems
            .AsNoTracking()
            .Where(e => string.Compare(e.CapturedAtUtc, fromUtc) >= 0 && string.Compare(e.CapturedAtUtc, toUtc) <= 0
                     && e.SourceType == sourceType.ToString());

        // Filter by OwnerUserId: only items owned by the query user, or items visible to all
        if (!string.IsNullOrEmpty(query.UserId))
        {
            dbQuery = dbQuery.Where(e => e.OwnerUserId == null || e.OwnerUserId == query.UserId);
        }

        if (statusFilter.HasValue)
        {
            var statusStr = statusFilter.Value.ToString();
            dbQuery = dbQuery.Where(e => e.Status == statusStr);
        }

        var entities = await dbQuery
            .OrderByDescending(e => e.CapturedAtUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToList();
    }

    private static WorkItem MapToDomain(Persistence.WorkItemEntity e)
    {
        var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(e.MetadataJson)
                       ?? new Dictionary<string, string>();

        return new WorkItem(
            e.ExternalId,
            e.Title,
            e.Source,
            Enum.Parse<WorkItemSourceType>(e.SourceType),
            Enum.Parse<WorkItemPriority>(e.Priority),
            metadata,
            e.CorrelationId,
            DateTimeOffset.Parse(e.CapturedAtUtc),
            priorityScore: e.PriorityScore,
            ownerUserId: e.OwnerUserId);
    }
}
