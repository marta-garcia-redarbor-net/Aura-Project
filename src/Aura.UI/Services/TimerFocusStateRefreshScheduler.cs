namespace Aura.UI.Services;

internal sealed class TimerFocusStateRefreshScheduler : IFocusStateRefreshScheduler
{
    public IDisposable StartRecurring(TimeSpan interval, Func<Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        return new Timer(async _ => await callback(), null, interval, interval);
    }
}
