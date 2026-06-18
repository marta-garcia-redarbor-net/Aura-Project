using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface IApiReadinessProvider
{
    Task<ReadinessSignal> GetReadinessAsync(CancellationToken cancellationToken);
}
