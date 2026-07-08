using Aura.Application.Ports;
using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Adapters.Rules;

/// <summary>
/// EF Core-backed store for dynamically managed alert rules (VIP senders and keywords).
/// Uses the <c>AlertRules</c> table with a <c>RuleType</c> discriminator via <see cref="AuraDbContext"/>.
/// </summary>
internal sealed class EfAlertRuleStore : IAlertRuleStore
{
    private const string VipSenderRuleType = "VipSender";
    private const string KeywordRuleType = "Keyword";

    private readonly AuraDbContext _db;

    public EfAlertRuleStore(AuraDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<IReadOnlyList<string>> GetVipSendersAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return await _db.AlertRules
            .AsNoTracking()
            .Where(e => e.RuleType == VipSenderRuleType)
            .OrderBy(e => e.Key)
            .Select(e => e.Key)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<string>> GetKeywordsAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return await _db.AlertRules
            .AsNoTracking()
            .Where(e => e.RuleType == KeywordRuleType)
            .OrderBy(e => e.Key)
            .Select(e => e.Key)
            .ToListAsync(ct);
    }

    public async Task AddVipSenderAsync(string email, string addedBy, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existing = await _db.AlertRules
            .FirstOrDefaultAsync(e => e.Key == email && e.RuleType == VipSenderRuleType, ct);

        if (existing is not null)
            return;

        _db.AlertRules.Add(new Persistence.AlertRule
        {
            Key = email,
            Value = email,
            AddedBy = addedBy,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            RuleType = VipSenderRuleType
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveVipSenderAsync(string email, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existing = await _db.AlertRules
            .FirstOrDefaultAsync(e => e.Key == email && e.RuleType == VipSenderRuleType, ct);

        if (existing is not null)
        {
            _db.AlertRules.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task AddKeywordAsync(string keyword, string addedBy, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existing = await _db.AlertRules
            .FirstOrDefaultAsync(e => e.Key == keyword && e.RuleType == KeywordRuleType, ct);

        if (existing is not null)
            return;

        _db.AlertRules.Add(new Persistence.AlertRule
        {
            Key = keyword,
            Value = keyword,
            AddedBy = addedBy,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            RuleType = KeywordRuleType
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveKeywordAsync(string keyword, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var existing = await _db.AlertRules
            .FirstOrDefaultAsync(e => e.Key == keyword && e.RuleType == KeywordRuleType, ct);

        if (existing is not null)
        {
            _db.AlertRules.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }
    }
}
