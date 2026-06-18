using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface IModuleProgressProvider
{
    Task<ModuleProgressDto> GetAsync(CancellationToken cancellationToken);
}
