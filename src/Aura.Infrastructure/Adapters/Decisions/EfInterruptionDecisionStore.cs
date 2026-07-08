using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Adapters.Decisions;

/// <summary>
/// EF Core-backed store for interruption decisions (audit trail).
/// Uses the <c>InterruptionDecisions</c> table via <see cref="AuraDbContext"/>.
/// </summary>
internal sealed class EfInterruptionDecisionStore : IInterruptionDecisionStore
{
    private readonly AuraDbContext _db;

    public EfInterruptionDecisionStore(AuraDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task RecordAsync(InterruptionDecisionRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        cancellationToken.ThrowIfCancellationRequested();

        _db.InterruptionDecisions.Add(new Persistence.InterruptionDecision
        {
            Id = Guid.NewGuid().ToString(),
            WorkItemId = record.WorkItemId.ToString(),
            Title = record.Title,
            SourceType = record.SourceType,
            Decision = record.Decision,
            PriorityScore = record.PriorityScore,
            Explanation = record.Explanation,
            Timestamp = record.Timestamp.ToString("O"),
            FocusState = record.FocusState
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<InterruptionDecisionRecord>> QueryAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;

        var totalCount = await _db.InterruptionDecisions.CountAsync(cancellationToken);

        var entities = await _db.InterruptionDecisions
            .AsNoTracking()
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(e => new InterruptionDecisionRecord(
                Guid.Parse(e.WorkItemId),
                e.Title,
                e.SourceType,
                e.Decision,
                e.PriorityScore,
                e.Explanation ?? string.Empty,
                DateTimeOffset.Parse(e.Timestamp),
                e.FocusState))
            .ToList();

        return new PagedResult<InterruptionDecisionRecord>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
