using Aura.Application.Ports;
using Aura.Domain.FocusState;
using Aura.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Adapters.FocusState;

/// <summary>
/// EF Core-backed store for user-defined focus state overrides.
/// Uses the <c>FocusStateOverrides</c> table via <see cref="AuraDbContext"/>.
/// </summary>
internal sealed class EfFocusStateOverrideStore : IFocusStateOverrideStore
{
    private readonly AuraDbContext _db;

    public EfFocusStateOverrideStore(AuraDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task<FocusStateType?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await _db.FocusStateOverrides
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

        if (entity is null)
            return null;

        return Enum.Parse<FocusStateType>(entity.State);
    }

    public async Task SetAsync(string userId, FocusStateType state)
    {
        var existing = await _db.FocusStateOverrides
            .FirstOrDefaultAsync(e => e.UserId == userId);

        var now = DateTimeOffset.UtcNow.ToString("O");

        if (existing is not null)
        {
            existing.State = state.ToString();
            existing.UpdatedAt = now;
        }
        else
        {
            _db.FocusStateOverrides.Add(new Persistence.FocusStateOverride
            {
                UserId = userId,
                State = state.ToString(),
                CreatedAt = now,
                UpdatedAt = null
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task ClearAsync(string userId)
    {
        var existing = await _db.FocusStateOverrides
            .FirstOrDefaultAsync(e => e.UserId == userId);

        if (existing is not null)
        {
            _db.FocusStateOverrides.Remove(existing);
            await _db.SaveChangesAsync();
        }
    }
}
