namespace Aura.Application.Ports;

/// <summary>
/// Read-only index of authenticated real-user oids present in the Graph/MSAL token cache.
/// Implementations are infrastructure adapters backed by token-cache persistence.
/// </summary>
public interface IGraphTokenCacheIndex
{
    Task<IReadOnlyList<string>> ListCachedUserOidsAsync(CancellationToken ct);
}
