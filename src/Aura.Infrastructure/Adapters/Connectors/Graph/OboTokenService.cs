using Aura.Infrastructure.Adapters.GraphConnector;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// Acquires Graph tokens via On-Behalf-Of (OBO) flow and caches them in the
/// <see cref="UserTokenStore"/> for the worker to consume.
///
/// The API receives the user's JWT in the Authorization header; OBO exchanges it
/// for a token with Mail.Read/Chat.Read/Calendars.Read scopes, which the worker
/// uses to call Graph directly without sharing MSAL public client state.
/// </summary>
public sealed class OboTokenService
{
    private readonly IConfidentialClientApplication _confidentialApp;
    private readonly UserTokenStore _userTokenStore;
    private readonly string[] _scopes;
    private readonly ILogger<OboTokenService> _logger;

    public OboTokenService(
        IConfidentialClientApplication confidentialApp,
        UserTokenStore userTokenStore,
        IOptions<GraphConnectorOptions> options,
        ILogger<OboTokenService> logger)
    {
        ArgumentNullException.ThrowIfNull(confidentialApp);
        ArgumentNullException.ThrowIfNull(userTokenStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _confidentialApp = confidentialApp;
        _userTokenStore = userTokenStore;
        _logger = logger;

        var opts = options.Value;
        var configuredScopes = opts.Scopes ?? ["Mail.Read", "Chat.Read"];
        _scopes = configuredScopes.Concat(["User.Read", "Calendars.Read"]).Distinct().ToArray();
    }

    /// <summary>
    /// Exchanges the user's JWT (bearer token from the HTTP request) for a Graph
    /// token via OBO and caches it in the UserTokenStore.
    /// </summary>
    /// <param name="userOid">The user's Azure AD object ID.</param>
    /// <param name="userBearerToken">The user's JWT from the Authorization header.</param>
    /// <returns>True when the token was acquired and cached successfully; false otherwise.</returns>
    public async Task<bool> CacheTokenForUserAsync(string userOid, string userBearerToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userOid);
        ArgumentException.ThrowIfNullOrWhiteSpace(userBearerToken);

        _logger.LogInformation("OBO token acquisition started for oid={Oid}", userOid);

        try
        {
            var userAssertion = new UserAssertion(userBearerToken);
            var result = await _confidentialApp
                .AcquireTokenOnBehalfOf(_scopes, userAssertion)
                .ExecuteAsync();

            _userTokenStore.SaveToken(userOid, result.AccessToken, result.ExpiresOn);

            _logger.LogInformation("OBO token cached for oid={Oid}, expires at {ExpiresOn}", userOid, result.ExpiresOn);
            return true;
        }
        catch (MsalUiRequiredException ex)
        {
            _logger.LogWarning("OBO requires user consent or re-authentication for oid={Oid}: {Message}. ErrorCode={ErrorCode}", userOid, ex.Message, ex.ErrorCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OBO token acquisition failed for oid={Oid}", userOid);
            return false;
        }
    }
}
