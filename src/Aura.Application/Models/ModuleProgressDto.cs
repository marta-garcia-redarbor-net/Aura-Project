namespace Aura.Application.Models;

public enum ModuleProgressState
{
    Pending,
    InProgress,
    Completed
}

public sealed record ModuleEntryDto(string ModuleId, ModuleProgressState State);

public sealed record ModuleProgressDto(IReadOnlyList<ModuleEntryDto> Entries, bool IsSeeded);
