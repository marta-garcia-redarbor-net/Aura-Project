using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.SemanticIndex.Enums;
using Aura.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Adapters.SemanticOutbox;

/// <summary>
/// EF Core-backed outbox repository for semantic index sync entries.
/// Uses the <c>SemanticOutbox</c> table via <see cref="AuraDbContext"/>.
/// </summary>
public sealed class EfSemanticOutboxRepository : ISemanticOutboxRepository
{
    private readonly AuraDbContext _db;

    public EfSemanticOutboxRepository(AuraDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task EnqueueAsync(SemanticOutboxEntry entry, CancellationToken ct)
    {
        _db.SemanticOutbox.Add(new Persistence.SemanticOutboxEntryEntity
        {
            Id = entry.Id.ToString(),
            CanonicalSourceId = entry.CanonicalSourceId,
            Content = entry.Content,
            Collection = (int)entry.Collection,
            CreatedAt = entry.CreatedAt.ToString("O"),
            Processed = entry.Processed,
            ProcessedAt = entry.ProcessedAt?.ToString("O"),
            Error = entry.Error
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SemanticOutboxEntry>> FetchPendingAsync(int batchSize, CancellationToken ct)
    {
        var entities = await _db.SemanticOutbox
            .AsNoTracking()
            .Where(e => !e.Processed)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);

        return entities.Select(MapToDomain).ToList();
    }

    public async Task UpdateAsync(SemanticOutboxEntry entry, CancellationToken ct)
    {
        var existing = await _db.SemanticOutbox
            .FirstOrDefaultAsync(e => e.Id == entry.Id.ToString(), ct);

        if (existing is not null)
        {
            existing.Processed = entry.Processed;
            existing.ProcessedAt = entry.ProcessedAt?.ToString("O");
            existing.Error = entry.Error;
            await _db.SaveChangesAsync(ct);
        }
    }

    private static SemanticOutboxEntry MapToDomain(Persistence.SemanticOutboxEntryEntity e)
    {
        var entry = new SemanticOutboxEntry(
            Guid.Parse(e.Id),
            e.CanonicalSourceId,
            e.Content,
            (SemanticCollectionType)e.Collection,
            DateTimeOffset.Parse(e.CreatedAt));

        if (e.Processed)
        {
            entry.MarkProcessed();
        }

        if (!string.IsNullOrEmpty(e.Error))
        {
            entry.MarkFailed(e.Error);
        }

        return entry;
    }
}
