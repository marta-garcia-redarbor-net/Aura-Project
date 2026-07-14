using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Identity.Client;

namespace Aura.UI.Services;

/// <summary>
/// Attaches a Bearer token to outbound API requests.
///
/// Token resolution order:
///   1. Existing Authorization header (from the browser request)
///   2. OIDC session access_token (SaveTokens=true in the OpenIdConnect options)
///   3. Cookie identity "token" claim (demo login or OIDC token persistence)
///   4. Client-credentials fallback via IConfidentialClientApplication (HttpContext path only)
///   5. Blazor circuit-safe fallback via AuthenticationStateProvider — reads the "token" claim
///      when HttpContext is null (Blazor Server interactive mode via SignalR)
/// </summary>
public sealed class ForwardedAccessTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ForwardedAccessTokenHandler> _logger;
    private readonly AuthenticationStateProvider _authStateProvider;

    // Cached from the HTTP request path (steps 1–3) where HttpContext is available.
    // Reused in the Blazor circuit path (step 5) where HttpContext is null and the
    // AuthenticationStateProvider is resolved from IHttpClientFactory's internal scope
    // (not the circuit scope), so it never receives SetCircuitUser() and cannot return claims.
    private string? _cachedToken;

    public ForwardedAccessTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<ForwardedAccessTokenHandler> logger,
        AuthenticationStateProvider authStateProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            HttpContext? httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is not null)
            {
            // 1. Existing Authorization header (dev mode / mock JWT)
            string? authorization = httpContext.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authorization) &&
                AuthenticationHeaderValue.TryParse(authorization, out AuthenticationHeaderValue? headerValue))
            {
                _logger.LogDebug("Forwarded existing Authorization header for {Uri}", request.RequestUri);
                request.Headers.Authorization = headerValue;
                return await base.SendAsync(request, cancellationToken);
            }

            // 2. OIDC session token (SaveTokens=true)
            string? accessToken = await httpContext.GetTokenAsync("access_token");
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogDebug("ForwardedAccessTokenHandler: Using OIDC session token for {Uri}", request.RequestUri);
                _cachedToken = accessToken;
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);
                return await base.SendAsync(request, cancellationToken);
            }

            // 3. Cookie identity "token" claim (dev/demo login or OIDC token storage)
            var tokenClaim = httpContext.User.FindFirstValue("token");
            if (!string.IsNullOrWhiteSpace(tokenClaim))
            {
                _logger.LogDebug("ForwardedAccessTokenHandler: Using cookie token claim for {Uri}", request.RequestUri);
                _cachedToken = tokenClaim;
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", tokenClaim);
                return await base.SendAsync(request, cancellationToken);
            }

            _logger.LogDebug(
                "No access_token in OIDC session for {Uri}. Attempting client-credentials fallback.",
                request.RequestUri);

                // 4. Client-credentials fallback (production)
                try
                {
                    var msalClient = httpContext.RequestServices.GetService<IConfidentialClientApplication>();
                    if (msalClient is not null)
                    {
                        var clientId = _configuration["AzureAd:ClientId"];
                        var scopes = new[] { $"api://{clientId}/.default" };
                        var result = await msalClient
                            .AcquireTokenForClient(scopes)
                            .ExecuteAsync(cancellationToken);

                        _logger.LogDebug(
                            "Client-credentials token acquired for {Uri} (audience: api://{ClientId})",
                            request.RequestUri, clientId);
                        request.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", result.AccessToken);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "IConfidentialClientApplication not available for {Uri}. No token will be sent.",
                            request.RequestUri);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Client-credentials token acquisition FAILED for {Uri}",
                        request.RequestUri);
                }
            }
            else
            {
                // 5. Blazor circuit path: HttpContext is null (SignalR circuit).
                //    First try the token cached from the last HTTP request (steps 2-3).
                //    The AuthenticationStateProvider fallback below uses IHttpClientFactory's
                //    internal DI scope which never receives SetCircuitUser(), so it cannot
                //    return claims reliably — the cache is the correct primary source here.
                if (!string.IsNullOrWhiteSpace(_cachedToken))
                {
                    _logger.LogDebug(
                        "ForwardedAccessTokenHandler: Using cached token for {Uri}",
                        request.RequestUri);
                    request.Headers.Authorization =
                        new AuthenticationHeaderValue("Bearer", _cachedToken);
                    return await base.SendAsync(request, cancellationToken);
                }

                _logger.LogWarning("HttpContext is NULL and no cached token for {Uri} — trying AuthenticationStateProvider fallback", request.RequestUri);

                // 5b. Last-resort: AuthenticationStateProvider (may not work due to DI scope isolation)
                try
                {
                    var state = await _authStateProvider.GetAuthenticationStateAsync();
                    var tokenClaim = state.User.FindFirstValue("token");
                    if (!string.IsNullOrWhiteSpace(tokenClaim))
                    {
                        _logger.LogDebug(
                            "ForwardedAccessTokenHandler: Using AuthenticationStateProvider token claim for {Uri}",
                            request.RequestUri);
                        _cachedToken = tokenClaim;
                        request.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", tokenClaim);
                        return await base.SendAsync(request, cancellationToken);
                    }

                    _logger.LogDebug(
                        "AuthenticationStateProvider had no 'token' claim for {Uri}", request.RequestUri);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "AuthenticationStateProvider fallback failed for {Uri}", request.RequestUri);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ForwardedAccessTokenHandler.SendAsync EXCEPTION for {Uri}", request.RequestUri);
            throw;
        }
    }
}
