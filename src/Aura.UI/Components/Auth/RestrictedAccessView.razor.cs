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
    /// Opens Microsoft OIDC login via full-page redirect.
    /// Blazor Server cannot use popups because SignalR breaks the synchronous
    /// click-to-popup chain required by browsers — window.open is always blocked.
    /// A redirect ensures the OIDC flow completes reliably.
    /// </summary>
    private void HandleMicrosoftLogin()
    {
        Navigation.NavigateTo("/login/challenge", forceLoad: true);
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
