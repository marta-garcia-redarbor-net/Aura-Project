using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface ILlmReadinessProvider
{
    Task<ReadinessSignal> GetReadinessAsync(CancellationToken cancellationToken);
}
