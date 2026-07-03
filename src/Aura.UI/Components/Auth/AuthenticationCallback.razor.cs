using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aura.UI.Components.Auth;

/// <summary>
/// Handles the OIDC authentication callback from the popup flow.
/// The OIDC middleware already exchanged the code at /signin-oidc and wrote the auth cookie.
/// Uses a ?popup=true query parameter (set by the challenge endpoint) to determine
/// whether we're in a popup — avoids unreliable JS window.opener detection across origins.
/// </summary>
public partial class AuthenticationCallback : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ILogger<AuthenticationCallback> Logger { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        bool isPopup = Navigation.Uri.Contains("popup=true", StringComparison.Ordinal);

        if (isPopup)
        {
            Logger.LogInformation("Auth callback in popup context — posting auth-success and closing.");

            // Try to notify the opener via postMessage (may fail for reasons unrelated to popup status)
            try
            {
                await JSRuntime.InvokeVoidAsync(
                    "eval",
                    "if(window.opener && !window.opener.closed){window.opener.postMessage({ type: 'auth-success' }, '*');}");
            }
            catch (JSException ex)
            {
                Logger.LogWarning(ex, "Could not post auth-success to opener — the main window may time out.");
            }

            // Close the popup — this is the main goal
            try
            {
                await JSRuntime.InvokeVoidAsync("eval", "window.close();");
            }
            catch (JSException ex)
            {
                Logger.LogWarning(ex, "Could not close popup via JS — user may need to close manually.");
            }
        }
        else
        {
            Logger.LogInformation("Auth callback in non-popup context — redirecting to /.");
            Navigation.NavigateTo("/", forceLoad: true);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
