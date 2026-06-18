using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Infrastructure.Adapters.Dashboard;

internal sealed class AlwaysHealthyApiReadinessAdapter : IApiReadinessProvider
{
    public Task<ReadinessSignal> GetReadinessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ReadinessSignal.Healthy);
    }
}
