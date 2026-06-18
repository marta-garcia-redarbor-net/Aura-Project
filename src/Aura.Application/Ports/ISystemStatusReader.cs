using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface ISystemStatusReader
{
    Task<SystemStatusDto> GetStatusAsync(CancellationToken cancellationToken);
}
