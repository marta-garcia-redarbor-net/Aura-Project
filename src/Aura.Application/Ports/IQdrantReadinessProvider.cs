using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface IQdrantReadinessProvider
{
    Task<ReadinessSignal> GetReadinessAsync(CancellationToken cancellationToken);
}
