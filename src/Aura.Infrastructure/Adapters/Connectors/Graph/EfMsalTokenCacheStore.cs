using Aura.Application.Ports;
using Aura.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// EF Core-backed MSAL token cache store. Stores serialized MSAL cache blobs keyed by cache key.
/// Uses the <c>MsalTokenCache</c> table via <see cref="AuraDbContext"/>.
/// </summary>
internal sealed class EfMsalTokenCacheStore : IMsalTokenCacheStore
{
    private readonly AuraDbContext _db;

    public EfMsalTokenCacheStore(AuraDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task PersistAsync(string cacheKey, byte[] data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        ArgumentNullException.ThrowIfNull(data);

        var existing = await _db.MsalTokenCache
            .FirstOrDefaultAsync(e => e.CacheKey == cacheKey);

        if (existing is not null)
        {
            existing.Data = data;
            existing.UpdatedAt = DateTimeOffset.UtcNow.ToString("O");
        }
        else
        {
            _db.MsalTokenCache.Add(new Persistence.MsalTokenCacheEntry
            {
                CacheKey = cacheKey,
                Data = data,
                UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<byte[]?> RetrieveAsync(string cacheKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        var entity = await _db.MsalTokenCache
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CacheKey == cacheKey);

        return entity?.Data;
    }

    public async Task<bool> HasCachedDataAsync(string cacheKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        return await _db.MsalTokenCache
            .AsNoTracking()
            .AnyAsync(e => e.CacheKey == cacheKey);
    }
}
