using Aura.Infrastructure.Adapters.GraphConnector;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Aura.Infrastructure.Adapters.Connectors.Graph;

/// <summary>
/// Creates authenticated <see cref="GraphServiceClient"/> instances using MSAL delegated tokens.
/// The factory uses AcquireTokenSilent to get cached tokens; if silent acquisition fails,
/// consumers should surface re-authentication need to the UI.
/// </summary>
internal sealed class GraphClientFactory : IGraphClientFactory
{
    private readonly IPublicClientApplication _msalApp;
    private readonly string[] _scopes;

    public GraphClientFactory(IPublicClientApplication msalApp, IOptions<GraphConnectorOptions> options)
    {
        ArgumentNullException.ThrowIfNull(msalApp);
        ArgumentNullException.ThrowIfNull(options);

        var opts = options.Value;
        _msalApp = msalApp;
        _scopes = opts.Scopes ?? ["Mail.Read", "Chat.Read", "User.Read"];
    }

    /// <summary>
    /// Creates a <see cref="GraphServiceClient"/> using cached delegated tokens for the specified user.
    /// </summary>
    /// <param name="oid">The Azure AD object ID of the authenticated user.</param>
    /// <exception cref="MsalUiRequiredException">When no valid cached token is available for the given oid.</exception>
    public async Task<GraphServiceClient> CreateClientAsync(string oid, CancellationToken ct)
    {
        var accounts = await _msalApp.GetAccountsAsync();
        var account = accounts.FirstOrDefault(a => a.HomeAccountId.ObjectId == oid);

        if (account is null)
        {
            throw new MsalUiRequiredException("no_account",
                $"No cached account found for oid={oid}. User must authenticate via UI first.");
        }

        var result = await _msalApp.AcquireTokenSilent(_scopes, account)
            .ExecuteAsync(ct);

        var tokenProvider = new BaseBearerTokenAuthenticationProvider(
            new StaticAccessTokenProvider(result.AccessToken));

        return new GraphServiceClient(tokenProvider);
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
