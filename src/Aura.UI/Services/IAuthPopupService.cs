using Microsoft.JSInterop;

namespace Aura.UI.Services;

/// <summary>
/// Manages the browser-popup-based OIDC authentication flow.
/// Communicates between the Blazor component and the popup window via JS interop.
/// </summary>
public interface IAuthPopupService
{
    /// <summary>
    /// Opens a browser popup window navigating to the given OIDC authorization URL.
    /// </summary>
    Task OpenMicrosoftLoginPopupAsync(string authUrl);

    /// <summary>
    /// Waits for the popup to send back an auth result via postMessage.
    /// </summary>
    Task<AuthResult?> WaitForPopupResultAsync(CancellationToken ct);

    /// <summary>
    /// Initializes JS interop references. Must be called before opening popups.
    /// </summary>
    ValueTask InitializeAsync(IJSRuntime js);

    /// <summary>
    /// Cleans up event listeners and dotnet references.
    /// </summary>
    ValueTask DisposeAsync();
}
