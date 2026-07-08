namespace Aura.Application.Ports;

/// <summary>
/// Port for persisting and retrieving MSAL token cache blobs.
/// Abstracts the token cache storage so implementations can swap between
/// SQLite (local dev) and EF Core / Azure SQL (ACA).
/// </summary>
public interface IMsalTokenCacheStore
{
    /// <summary>Persists the MSAL cache blob for the given key (upsert).</summary>
    Task PersistAsync(string cacheKey, byte[] data);

    /// <summary>Retrieves the cached MSAL blob for the given key, or null if absent.</summary>
    Task<byte[]?> RetrieveAsync(string cacheKey);

    /// <summary>Checks if there is cached data for the given key.</summary>
    Task<bool> HasCachedDataAsync(string cacheKey);
}
