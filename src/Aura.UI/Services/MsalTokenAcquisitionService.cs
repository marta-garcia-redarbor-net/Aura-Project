using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Aura.UI.Services;

/// <summary>
/// Cookie-session implementation of <see cref="ITokenAcquisitionService"/>.
/// Reads the access token stored by the OIDC middleware (SaveTokens=true) from the HTTP
/// session cookie. Does NOT use AcquireTokenInteractive — that is a desktop primitive
/// incompatible with server-side Blazor.
/// </summary>
internal sealed class MsalTokenAcquisitionService : ITokenAcquisitionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MsalTokenAcquisitionService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor
            ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<string> AcquireTokenAsync(CancellationToken cancellationToken = default)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("No active HttpContext. Ensure IHttpContextAccessor is registered and the request is in scope.");

        string accessToken = await httpContext.GetTokenAsync("access_token")
            ?? throw new InvalidOperationException(
                "No access_token in session. Ensure SaveTokens=true is configured and the user is authenticated via the OIDC popup flow.");

        return accessToken;
    }
}
