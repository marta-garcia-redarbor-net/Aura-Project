using Aura.Infrastructure.Adapters.GraphConnector;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// Creates authenticated <see cref="GraphServiceClient"/> instances using MSAL delegated tokens
/// or fallback OBO tokens from the <see cref="UserTokenStore"/>.
///
/// Token resolution order:
///  1. MSAL public client cache (device code / interactive users) — preferred path.
///  2. UserTokenStore (OBO tokens acquired by the API sync endpoint) — fallback path.
///  3. MsalUiRequiredException — no token available, user must trigger sync via UI first.
/// </summary>
internal sealed partial class GraphClientFactory : IGraphClientFactory
{
    private readonly IPublicClientApplication _msalApp;
    private readonly UserTokenStore _userTokenStore;
    private readonly string[] _scopes;
    private readonly ILogger<GraphClientFactory> _logger;

    public GraphClientFactory(
        IPublicClientApplication msalApp,
        UserTokenStore userTokenStore,
        IOptions<GraphConnectorOptions> options,
        ILogger<GraphClientFactory>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(msalApp);
        ArgumentNullException.ThrowIfNull(userTokenStore);
        ArgumentNullException.ThrowIfNull(options);

        var opts = options.Value;
        _msalApp = msalApp;
        _userTokenStore = userTokenStore;
        _logger = logger ?? NullLogger<GraphClientFactory>.Instance;
        // Always include required scopes (User.Read for sign-in, Calendars.Read for meetings),
        // merged with feature-specific scopes from configuration.
        var configuredScopes = opts.Scopes ?? ["Mail.Read", "Chat.Read"];
        _scopes = configuredScopes.Concat(["User.Read", "Calendars.Read"]).Distinct().ToArray();
    }

    /// <summary>
    /// Creates a <see cref="GraphServiceClient"/> using cached delegated tokens for the specified user.
    /// Tries MSAL silent acquisition first, then falls back to the OBO token store.
    /// </summary>
    /// <param name="oid">The Azure AD object ID of the authenticated user.</param>
    /// <exception cref="MsalUiRequiredException">When no valid cached token is available for the given oid.</exception>
    public async Task<GraphServiceClient> CreateClientAsync(string oid, CancellationToken ct)
    {
        Log.TokenResolutionStarted(_logger, oid);

        // 1. Try MSAL public client cache (device code / interactive users)
        var accounts = await _msalApp.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a => a.HomeAccountId.ObjectId == oid);

        if (account is not null)
        {
            Log.MsalAccountResolved(_logger, oid);

            try
            {
                var result = await _msalApp.AcquireTokenSilent(_scopes, account)
                    .ExecuteAsync(ct);

                Log.MsalTokenResolved(_logger, oid);
                return CreateClientFromToken(result.AccessToken);
            }
            catch (MsalUiRequiredException)
            {
                // MSAL token expired with no refresh — fall through to OBO store
                Log.MsalUiRequiredFallbackToUserTokenStore(_logger, oid);
            }
        }
        else
        {
            Log.MsalAccountNotFound(_logger, oid);
        }

        // 2. Fallback: OBO-acquired token from UserTokenStore (acquired via API sync endpoint)
        var cached = _userTokenStore.GetToken(oid);
        if (cached is not null)
        {
            Log.UserTokenStoreHit(_logger, oid);
            var (accessToken, expiresAt) = cached.Value;

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // If expired, signal caller to re-acquire via OBO
                if (expiresAt is not null && expiresAt.Value <= DateTimeOffset.UtcNow)
                {
                    _userTokenStore.RemoveToken(oid);
                    Log.UserTokenStoreExpired(_logger, oid);
                    throw new MsalUiRequiredException("token_expired",
                        $"OBO token for oid={oid} has expired. Re-trigger sync from the UI to refresh.");
                }

                Log.UserTokenStoreTokenResolved(_logger, oid);
                return CreateClientFromToken(accessToken);
            }
        }
        else
        {
            Log.UserTokenStoreMiss(_logger, oid);
        }

        // 3. No token available in either source
        Log.NoTokenAvailable(_logger, oid);
        throw new MsalUiRequiredException("no_token",
            $"No cached token found for oid={oid}. Trigger a sync from the dashboard first.");
    }

    private static GraphServiceClient CreateClientFromToken(string accessToken)
    {
        var tokenProvider = new BaseBearerTokenAuthenticationProvider(
            new StaticAccessTokenProvider(accessToken));

        return new GraphServiceClient(tokenProvider);
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 3401, Level = Microsoft.Extensions.Logging.LogLevel.Information,
            Message = "Graph token resolution started for oid={Oid}")]
        public static partial void TokenResolutionStarted(ILogger logger, string oid);

        [LoggerMessage(EventId = 3402, Level = Microsoft.Extensions.Logging.LogLevel.Debug,
            Message = "Graph token resolution: MSAL account found for oid={Oid}")]
        public static partial void MsalAccountResolved(ILogger logger, string oid);

        [LoggerMessage(EventId = 3403, Level = Microsoft.Extensions.Logging.LogLevel.Information,
            Message = "Graph token resolution: MSAL silent token resolved for oid={Oid}")]
        public static partial void MsalTokenResolved(ILogger logger, string oid);

        [LoggerMessage(EventId = 3404, Level = Microsoft.Extensions.Logging.LogLevel.Warning,
            Message = "Graph token resolution: MSAL requires interaction for oid={Oid}, falling back to UserTokenStore")]
        public static partial void MsalUiRequiredFallbackToUserTokenStore(ILogger logger, string oid);

        [LoggerMessage(EventId = 3405, Level = Microsoft.Extensions.Logging.LogLevel.Information,
            Message = "Graph token resolution: no MSAL account found for oid={Oid}")]
        public static partial void MsalAccountNotFound(ILogger logger, string oid);

        [LoggerMessage(EventId = 3406, Level = Microsoft.Extensions.Logging.LogLevel.Information,
            Message = "Graph token resolution: UserTokenStore hit for oid={Oid}")]
        public static partial void UserTokenStoreHit(ILogger logger, string oid);

        [LoggerMessage(EventId = 3407, Level = Microsoft.Extensions.Logging.LogLevel.Warning,
            Message = "Graph token resolution: UserTokenStore token expired for oid={Oid}")]
        public static partial void UserTokenStoreExpired(ILogger logger, string oid);

        [LoggerMessage(EventId = 3408, Level = Microsoft.Extensions.Logging.LogLevel.Information,
            Message = "Graph token resolution: UserTokenStore token resolved for oid={Oid}")]
        public static partial void UserTokenStoreTokenResolved(ILogger logger, string oid);

        [LoggerMessage(EventId = 3409, Level = Microsoft.Extensions.Logging.LogLevel.Information,
            Message = "Graph token resolution: UserTokenStore miss for oid={Oid}")]
        public static partial void UserTokenStoreMiss(ILogger logger, string oid);

        [LoggerMessage(EventId = 3410, Level = Microsoft.Extensions.Logging.LogLevel.Warning,
            Message = "Graph token resolution failed: no cached token available for oid={Oid}")]
        public static partial void NoTokenAvailable(ILogger logger, string oid);
    }
}

/// <summary>
/// Simple token provider that returns a static access token.
/// Used to create a GraphServiceClient from a pre-acquired MSAL token.
/// </summary>
internal sealed class StaticAccessTokenProvider : IAccessTokenProvider
{
    private readonly string _accessToken;

    public StaticAccessTokenProvider(string accessToken)
    {
        _accessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
    }

    public AllowedHostsValidator AllowedHostsValidator { get; } = new();

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_accessToken);
    }
}
