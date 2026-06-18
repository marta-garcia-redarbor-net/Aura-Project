using Aura.UI.Models;

namespace Aura.UI.Services;

public interface IModuleProgressApiClient
{
    Task<ModuleProgressResponse> GetAsync(CancellationToken cancellationToken);
}
