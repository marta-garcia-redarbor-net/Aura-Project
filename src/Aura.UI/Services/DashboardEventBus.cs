namespace Aura.UI.Services;

/// <summary>
/// Bus de eventos para notificaciones del dashboard.
/// Singleton para que todos los componentes compartan el mismo bus y tracking.
/// </summary>
public interface IDashboardEventBus
{
    event Func<Task>? OnDashboardRefresh;
    Task RaiseDashboardRefreshAsync();
    bool IsNewItem(string itemId);
    void ClearTracking();
}

public class DashboardEventBus : IDashboardEventBus
{
    private readonly HashSet<string> _seenItemIds = new();
    private readonly object _lock = new();

    public event Func<Task>? OnDashboardRefresh;

    public async Task RaiseDashboardRefreshAsync()
    {
        if (OnDashboardRefresh is not null)
        {
            await OnDashboardRefresh.Invoke();
        }
    }

    public bool IsNewItem(string itemId)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(itemId) || _seenItemIds.Contains(itemId))
                return false;

            _seenItemIds.Add(itemId);
            return true;
        }
    }

    public void ClearTracking()
    {
        lock (_lock)
        {
            _seenItemIds.Clear();
        }
    }
}
