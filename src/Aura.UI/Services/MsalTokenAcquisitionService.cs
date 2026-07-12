using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Aura.UI.Services;

/// <summary>
/// Implementation of <see cref="ITokenAcquisitionService"/> that reads the access token
/// from the OIDC session cookie (via SaveTokens=true) with fallback to the "token" claim
/// stored by the demo/dev login flows.
///
/// Uses <see cref="AuthenticationStateProvider"/> (Blazor circuit-safe) instead of
/// <see cref="IHttpContextAccessor"/>, because in Blazor Server interactive mode the
/// HttpContext is null during component lifecycle.
/// </summary>
internal sealed class MsalTokenAcquisitionService : ITokenAcquisitionService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<MsalTokenAcquisitionService> _logger;
    private string? _cachedToken;

    public MsalTokenAcquisitionService(
        AuthenticationStateProvider authStateProvider,
        IHttpContextAccessor httpContextAccessor,
        ILogger<MsalTokenAcquisitionService> logger)
    {
        _authStateProvider = authStateProvider ?? throw new ArgumentNullException(nameof(authStateProvider));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> AcquireTokenAsync(CancellationToken cancellationToken = default)
    {
        // 1. Return cached token if available
        if (_cachedToken is not null)
            return _cachedToken;

        // 2. Try OIDC access_token from HTTP context (only available during initial HTTP request)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var oidcToken = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrEmpty(oidcToken))
            {
                _cachedToken = oidcToken;
                _logger.LogDebug("Using OIDC access_token for authentication");
                return _cachedToken;
            }
        }

        // 3. Fallback: try "token" claim from the authenticated identity (demo/dev login flows)
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        var tokenClaim = state.User.FindFirst("token")?.Value;
        if (!string.IsNullOrEmpty(tokenClaim))
        {
            _cachedToken = tokenClaim;
            _logger.LogDebug("Using demo/dev 'token' claim for authentication");
            return _cachedToken;
        }

        // 4. No token available
        throw new InvalidOperationException(
            "No access_token available. Ensure the user is authenticated via OIDC popup flow "
            + "(access_token claim) or demo/dev login (token claim).");
    }
}
