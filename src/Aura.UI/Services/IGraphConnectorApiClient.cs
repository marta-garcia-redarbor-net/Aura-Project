using Aura.UI.Models;

namespace Aura.UI.Services;

public interface IGraphConnectorApiClient
{
    Task<GraphConnectorStatusResponse> GetStatusAsync(CancellationToken cancellationToken);
}
