using Aura.Application.Ports;

namespace Aura.Application.Services;

internal sealed class NullDashboardRefreshDispatcher : IDashboardRefreshDispatcher
{
    public Task DispatchAsync(string? userId, CancellationToken ct) => Task.CompletedTask;
}
