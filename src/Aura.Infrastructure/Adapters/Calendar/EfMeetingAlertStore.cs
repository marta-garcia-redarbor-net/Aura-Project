using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Adapters.Calendar;

/// <summary>
/// EF Core-backed store for meeting alerts.
/// Uses the <c>MeetingAlerts</c> table via <see cref="AuraDbContext"/>.
/// </summary>
internal sealed class EfMeetingAlertStore : IMeetingAlertStore
{
    private readonly AuraDbContext _db;

    public EfMeetingAlertStore(AuraDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<MeetingAlert?> GetUnsentAlertAsync(string eventId, MeetingAlertTrigger trigger, DateTimeOffset date, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var localDate = date.ToString("yyyy-MM-dd");

        var entity = await _db.MeetingAlerts
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.EventId == eventId &&
                e.Trigger == trigger.ToString() &&
                e.LocalDate == localDate &&
                !e.HasBeenSent, ct);

        if (entity is null)
            return null;

        return MapToDomain(entity);
    }

    public async Task MarkSentAsync(MeetingAlert alert, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(alert);
        ct.ThrowIfCancellationRequested();

        var localDate = alert.StartsAtUtc.ToString("yyyy-MM-dd");

        var existing = await _db.MeetingAlerts
            .FirstOrDefaultAsync(e =>
                e.EventId == alert.EventId &&
                e.Trigger == alert.Trigger.ToString() &&
                e.LocalDate == localDate, ct);

        if (existing is not null)
        {
            existing.HasBeenSent = true;
        }
        else
        {
            _db.MeetingAlerts.Add(new Persistence.MeetingAlertEntity
            {
                EventId = alert.EventId,
                Trigger = alert.Trigger.ToString(),
                LocalDate = localDate,
                Title = alert.Title,
                StartsAtUtc = alert.StartsAtUtc.ToString("O"),
                JoinUrl = alert.JoinUrl,
                UserId = alert.UserId,
                HasBeenSent = true
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<MeetingAlert>> GetUpcomingAlertsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var fromUtc = from.ToString("O");
        var toUtc = to.ToString("O");

        var entities = await _db.MeetingAlerts
            .AsNoTracking()
            .Where(e => string.Compare(e.StartsAtUtc, fromUtc) >= 0 && string.Compare(e.StartsAtUtc, toUtc) <= 0)
            .OrderBy(e => e.StartsAtUtc)
            .ToListAsync(ct);

        return entities.Select(MapToDomain).ToList();
    }

    private static MeetingAlert MapToDomain(Persistence.MeetingAlertEntity e)
    {
        return new MeetingAlert(
            e.EventId,
            e.Title,
            Enum.Parse<MeetingAlertTrigger>(e.Trigger),
            DateTimeOffset.Parse(e.StartsAtUtc),
            e.JoinUrl,
            e.UserId,
            e.HasBeenSent);
    }
}
