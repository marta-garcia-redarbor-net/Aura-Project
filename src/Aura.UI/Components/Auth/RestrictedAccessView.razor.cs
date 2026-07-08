using Microsoft.AspNetCore.Components;

namespace Aura.UI.Components.Auth;

public partial class RestrictedAccessView : ComponentBase
{
    [Inject]
    private IConfiguration Configuration { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private bool _useEntraId;

    protected override void OnInitialized()
    {
        _useEntraId = Configuration.GetValue<bool>("UseEntraId");
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

    private void HandleDevLogin()
    {
        Navigation.NavigateTo("/login/dev", forceLoad: true);
    }
}
