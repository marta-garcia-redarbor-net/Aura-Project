namespace Aura.UI.Services;

/// <summary>
/// Scoped service that detects session expiration and notifies the layout
/// to redirect to the landing page. Works from any page, not just the dashboard.
/// </summary>
public sealed class SessionExpiredService
{
    public event Func<Task>? OnSessionExpired;

    public async Task NotifySessionExpiredAsync()
    {
        if (OnSessionExpired is not null)
            await OnSessionExpired.Invoke();
    }
}
