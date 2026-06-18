using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface IModuleProgressReader
{
    Task<ModuleProgressDto> GetAsync(CancellationToken cancellationToken);
}
