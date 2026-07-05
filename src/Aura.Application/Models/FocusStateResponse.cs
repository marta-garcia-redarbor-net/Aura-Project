namespace Aura.Application.Models;

/// <summary>
/// API response DTO for the current focus state, including any active override.
/// </summary>
public sealed record FocusStateResponse
{
    /// <summary>
    /// The effective focus state (derived from override or automatic resolution).
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// Whether this state is an explicit user override.
    /// </summary>
    public bool IsOverridden { get; init; }

    /// <summary>
    /// Current authenticated user identity resolved for this focus state.
    /// </summary>
    public required string UserId { get; init; }
}
