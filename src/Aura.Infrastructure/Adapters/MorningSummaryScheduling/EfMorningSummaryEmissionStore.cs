using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Aura.Infrastructure.Adapters.MorningSummaryScheduling;

/// <summary>
/// EF Core-backed store for morning summary emission tracking.
/// Uses the <c>MorningSummaryEmission</c> table via <see cref="AuraDbContext"/>.
/// </summary>
internal sealed class EfMorningSummaryEmissionStore : IMorningSummaryEmissionStore
{
    private readonly AuraDbContext _db;

    public EfMorningSummaryEmissionStore(AuraDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<bool> HasBeenEmittedAsync(string userId, DateOnly localDate, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var formattedDate = FormatLocalDate(localDate);

        return await _db.MorningSummaryEmission
            .AsNoTracking()
            .AnyAsync(e => e.UserId == userId && e.LocalDate == formattedDate, ct);
    }

    public async Task MarkEmittedAsync(string userId, DateOnly localDate, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var formattedDate = FormatLocalDate(localDate);

        var existing = await _db.MorningSummaryEmission
            .FirstOrDefaultAsync(e => e.UserId == userId && e.LocalDate == formattedDate, ct);

        if (existing is not null)
            return;

        _db.MorningSummaryEmission.Add(new Persistence.MorningSummaryEmission
        {
            UserId = userId,
            LocalDate = formattedDate,
            EmittedAt = DateTimeOffset.UtcNow.ToString("O")
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task ResetAsync(string userId, DateOnly localDate, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var formattedDate = FormatLocalDate(localDate);

        var existing = await _db.MorningSummaryEmission
            .FirstOrDefaultAsync(e => e.UserId == userId && e.LocalDate == formattedDate, ct);

        if (existing is not null)
        {
            _db.MorningSummaryEmission.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }
    }

    private static string FormatLocalDate(DateOnly localDate)
        => localDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
