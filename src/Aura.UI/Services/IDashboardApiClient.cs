using Aura.UI.Models;

namespace Aura.UI.Services;

public interface IDashboardApiClient
{
    Task<InitialDashboardResponse> GetInitialDashboardAsync(CancellationToken cancellationToken);
}
