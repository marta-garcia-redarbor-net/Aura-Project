using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface IDbReadinessProvider
{
    Task<ReadinessSignal> GetReadinessAsync(CancellationToken cancellationToken);
}
