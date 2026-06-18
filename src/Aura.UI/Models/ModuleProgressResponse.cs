namespace Aura.UI.Models;

public enum ModuleProgressStateResponse
{
    Pending,
    InProgress,
    Completed
}

public sealed record ModuleEntryResponse(string ModuleId, ModuleProgressStateResponse State);

public sealed record ModuleProgressResponse(IReadOnlyList<ModuleEntryResponse> Entries, bool IsSeeded);
