using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Application.Services;

public sealed class ModuleProgressReader : IModuleProgressReader
{
    private readonly IModuleProgressProvider _moduleProgressProvider;

    public ModuleProgressReader(IModuleProgressProvider moduleProgressProvider)
    {
        ArgumentNullException.ThrowIfNull(moduleProgressProvider);
        _moduleProgressProvider = moduleProgressProvider;
    }

    public Task<ModuleProgressDto> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _moduleProgressProvider.GetAsync(cancellationToken);
    }
}
