using Aura.UI.Models;

namespace Aura.UI.Services;

public interface ISystemStatusApiClient
{
    Task<SystemStatusResponse> GetStatusAsync(CancellationToken cancellationToken);

    Task<List<ErrorEntryDto>> GetRecentErrorsAsync(CancellationToken cancellationToken);
}
