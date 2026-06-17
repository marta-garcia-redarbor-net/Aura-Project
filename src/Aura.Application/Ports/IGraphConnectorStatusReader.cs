using Aura.Application.Models;

namespace Aura.Application.Ports;

public interface IGraphConnectorStatusReader
{
    Task<GraphConnectorStatusDto> GetStatusAsync(CancellationToken cancellationToken);
}
