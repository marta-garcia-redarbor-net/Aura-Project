namespace Aura.UI.Services;

public interface IDashboardRealtimeStatus
{
    bool IsConnected { get; }
    DateTimeOffset? LastRefreshUtc { get; }
    DateTimeOffset? LastAttemptUtc { get; }
    string LastState { get; }
    void MarkAttempt(string state);
    void MarkConnected();
    void MarkDisconnected(string state);
    void MarkRefreshReceived();
}

public sealed class DashboardRealtimeStatus : IDashboardRealtimeStatus
{
    public bool IsConnected { get; private set; }
    public DateTimeOffset? LastRefreshUtc { get; private set; }
    public DateTimeOffset? LastAttemptUtc { get; private set; }
    public string LastState { get; private set; } = "idle";

    public void MarkAttempt(string state)
    {
        LastAttemptUtc = DateTimeOffset.UtcNow;
        LastState = state;
    }

    public void MarkConnected()
    {
        IsConnected = true;
        LastAttemptUtc = DateTimeOffset.UtcNow;
        LastState = "connected";
    }

    public void MarkDisconnected(string state)
    {
        IsConnected = false;
        LastAttemptUtc = DateTimeOffset.UtcNow;
        LastState = state;
    }

    public void MarkRefreshReceived()
    {
        LastRefreshUtc = DateTimeOffset.UtcNow;
        LastState = "refresh-received";
    }
}
