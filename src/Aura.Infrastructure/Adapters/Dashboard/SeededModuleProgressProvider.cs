using Aura.Application.Models;
using Aura.Application.Ports;

namespace Aura.Infrastructure.Adapters.Dashboard;

internal sealed class SeededModuleProgressProvider : IModuleProgressProvider
{
    private static readonly IReadOnlyList<ModuleEntryDto> Entries =
    [
        new("w1-ingestion", ModuleProgressState.Completed),
        new("w1-triage", ModuleProgressState.InProgress),
        new("w1-reviewer", ModuleProgressState.Pending)
    ];

    public Task<ModuleProgressDto> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new ModuleProgressDto(Entries, IsSeeded: true));
    }
}
