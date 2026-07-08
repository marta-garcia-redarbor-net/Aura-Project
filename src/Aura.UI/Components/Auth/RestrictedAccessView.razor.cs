using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Aura.UI.Services;

namespace Aura.UI.Components.Auth;

public partial class RestrictedAccessView : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IAuthPopupService AuthPopupService { get; set; } = default!;

    [Inject]
    private IConfiguration Configuration { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    private bool _useEntraId;
    private string? _errorMessage;
    private bool _initialized;
    private bool _popupBlocked;
    private string? _blockedToastMessage;

    protected override void OnInitialized()
    {
        _useEntraId = Configuration.GetValue<bool>("UseEntraId");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await AuthPopupService.InitializeAsync(JSRuntime);
        _initialized = true;
    }

    /// <summary>
    /// Returns the challenge endpoint URL with popup context indicator.
    /// The OIDC middleware owns state/nonce/correlation.
    /// The ?popup=true query param survives the full OIDC redirect chain and
    /// tells the callback page it should close itself instead of redirecting to /.
    /// </summary>
    private static string BuildAuthUrl() => "/login/challenge?popup=true";

    private async Task HandleMicrosoftLogin()
    {
        try
        {
            _errorMessage = null;
            _popupBlocked = false;
            _blockedToastMessage = null;

            string authUrl = BuildAuthUrl();
            await AuthPopupService.OpenMicrosoftLoginPopupAsync(authUrl);

            AuthResult? result = await AuthPopupService.WaitForPopupResultAsync(CancellationToken.None);

            if (result is { Success: true })
            {
                Navigation.NavigateTo("/", forceLoad: true);
            }
            else if (result is not null)
            {
                _errorMessage = result.Error ?? "Authentication failed. Please try again.";
                StateHasChanged();
            }
        }
        catch (InvalidOperationException)
        {
            _popupBlocked = true;
            _blockedToastMessage = "Pop-up blocked. Redirecting to login...";
            StateHasChanged();
        }
        catch (JSException)
        {
            _popupBlocked = true;
            _blockedToastMessage = "Pop-up blocked. Redirecting to login...";
            StateHasChanged();
        }
    }

    private void HandleFallbackRedirect()
    {
        Navigation.NavigateTo("/login/challenge", forceLoad: true);
    }

    private void HandleDevLogin()
    {
        Navigation.NavigateTo("/login/dev", forceLoad: true);
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_initialized)
        {
            await AuthPopupService.DisposeAsync();
        }
    }
}
