using Aura.UI.Models;

namespace Aura.UI.Services;

public interface IFocusStateApiClient
{
    Task<FocusStateResponse> GetCurrentAsync(CancellationToken cancellationToken);
}
