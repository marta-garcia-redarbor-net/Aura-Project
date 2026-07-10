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
    void MarkAsSeen(string itemId);
    void SeedItems(IEnumerable<string> itemIds);
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
            return !string.IsNullOrEmpty(itemId) && !_seenItemIds.Contains(itemId);
        }
    }

    public void MarkAsSeen(string itemId)
    {
        lock (_lock)
        {
            if (!string.IsNullOrEmpty(itemId))
                _seenItemIds.Add(itemId);
        }
    }

    public void SeedItems(IEnumerable<string> itemIds)
    {
        lock (_lock)
        {
            foreach (var id in itemIds)
            {
                if (!string.IsNullOrEmpty(id))
                    _seenItemIds.Add(id);
            }
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
