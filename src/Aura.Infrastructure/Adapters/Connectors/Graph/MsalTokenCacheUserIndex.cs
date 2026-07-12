using Aura.Application.Ports;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

internal sealed class MsalTokenCacheUserIndex : IGraphTokenCacheIndex
{
    private readonly MsalSqliteTokenCache _tokenCache;

    public MsalTokenCacheUserIndex(MsalSqliteTokenCache tokenCache)
    {
        _tokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));
    }

    public Task<IReadOnlyList<string>> ListCachedUserOidsAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_tokenCache.ListUserOids());
    }
}
