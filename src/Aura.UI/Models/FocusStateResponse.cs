namespace Aura.UI.Models;

public sealed record FocusStateResponse(
    string CurrentState,
    string? Label,
    DateTimeOffset Since,
    IReadOnlyList<string> Signals);
