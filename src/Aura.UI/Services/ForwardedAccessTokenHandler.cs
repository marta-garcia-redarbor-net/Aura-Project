using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Identity.Client;

namespace Aura.UI.Services;

/// <summary>
/// Attaches a Bearer token to outbound API requests.
///
/// Token resolution order:
///   1. Existing Authorization header (from the browser request)
///   2. OIDC session access_token (SaveTokens=true in the OpenIdConnect options)
///   3. Cookie identity "token" claim (demo login or OIDC token persistence)
///   4. Client-credentials fallback via IConfidentialClientApplication
/// </summary>
public sealed class ForwardedAccessTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ForwardedAccessTokenHandler> _logger;

    public ForwardedAccessTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<ForwardedAccessTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
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
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);
                return await base.SendAsync(request, cancellationToken);
            }

            // 3. Cookie identity "token" claim (dev/demo login or OIDC token storage)
            var tokenClaim = httpContext.User.FindFirstValue("token");
            if (!string.IsNullOrWhiteSpace(tokenClaim))
            {
                _logger.LogDebug("ForwardedAccessTokenHandler: Using cookie token claim for {Uri}", request.RequestUri);
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
                _logger.LogWarning("HttpContext is NULL — no token attached for {Uri}", request.RequestUri);
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
