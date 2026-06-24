using Aura.Application.Models;

namespace Aura.Application.Ports;

/// <summary>
/// Port to query the validity of cached authentication tokens.
/// Consumers (e.g., workers) use this to determine whether re-authentication is needed
/// without coupling to any specific token provider (e.g., MSAL).
/// </summary>
public interface ITokenCacheStatus
{
    Task<TokenStatus> GetStatusAsync(CancellationToken ct);
}
