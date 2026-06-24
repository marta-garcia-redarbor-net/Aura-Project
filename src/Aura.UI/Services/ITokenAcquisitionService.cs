namespace Aura.UI.Services;

/// <summary>
/// Port for acquiring access tokens for SignalR hub authentication.
/// Implementations provide MSAL interactive flow or dev fallback.
/// </summary>
public interface ITokenAcquisitionService
{
    /// <summary>
    /// Acquires an access token for the MeetingAlerts scope.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token string.</returns>
    Task<string> AcquireTokenAsync(CancellationToken cancellationToken = default);
}