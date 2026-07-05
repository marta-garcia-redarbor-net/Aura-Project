namespace Aura.UI.Services;

public interface IFocusStateRefreshScheduler
{
    IDisposable StartRecurring(TimeSpan interval, Func<Task> callback);
}
