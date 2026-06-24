using Aura.UI.Models;

namespace Aura.UI.Services;

public interface IDashboardPreviewApiClient
{
    Task<DashboardPreviewResponse> GetPreviewAsync(CancellationToken cancellationToken);
}
