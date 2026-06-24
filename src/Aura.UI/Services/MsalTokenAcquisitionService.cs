using Microsoft.Identity.Client;

namespace Aura.UI.Services;

/// <summary>
/// MSAL implementation of <see cref="ITokenAcquisitionService"/> using interactive browser flow.
/// Uses the same Entra ID App Registration as the Graph API connector with an additional
/// MeetingAlerts scope. Falls back to dev mock JWT when MSAL config is absent.
/// </summary>
internal sealed class MsalTokenAcquisitionService : ITokenAcquisitionService
{
    private readonly IPublicClientApplication _msalApp;
    private static readonly string[] MeetingAlertsScope = ["api://<app-id>/MeetingAlerts"];

    public MsalTokenAcquisitionService(IPublicClientApplication msalApp)
    {
        _msalApp = msalApp ?? throw new ArgumentNullException(nameof(msalApp));
    }

    public async Task<string> AcquireTokenAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _msalApp.GetAccountsAsync();
        var account = accounts.FirstOrDefault();

        var builder = _msalApp.AcquireTokenInteractive(MeetingAlertsScope);
        if (account is not null)
        {
            builder = builder.WithAccount(account);
        }

        var result = await builder.ExecuteAsync(cancellationToken);

        return result.AccessToken;
    }
}