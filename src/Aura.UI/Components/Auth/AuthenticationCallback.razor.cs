using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Aura.UI.Components.Auth;

/// <summary>
/// Handles the OIDC authentication callback from the popup flow.
/// The OIDC middleware already exchanged the code at /signin-oidc and wrote the auth cookie.
/// This component detects the popup context and either posts auth-success to the opener or
/// redirects to / for non-popup navigation.
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

        try
        {
            bool isPopup = await JSRuntime.InvokeAsync<bool>(
                "eval",
                "window.opener !== null && !window.opener.closed");

            if (isPopup)
            {
                Logger.LogInformation("Auth callback in popup context — posting auth-success and closing.");
                await JSRuntime.InvokeVoidAsync(
                    "eval",
                    "window.opener.postMessage({ type: 'auth-success' }, '*'); window.close();");
            }
            else
            {
                Logger.LogInformation("Auth callback in non-popup context — redirecting to /.");
                Navigation.NavigateTo("/", forceLoad: true);
            }
        }
        catch (JSException ex)
        {
            Logger.LogWarning(ex, "JS interop error in authentication callback — falling back to redirect.");
            Navigation.NavigateTo("/", forceLoad: true);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
