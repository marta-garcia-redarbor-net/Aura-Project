namespace Aura.Application.Models;

/// <summary>
/// Token cache status for background workers to determine if re-authentication is needed.
/// </summary>
public sealed record TokenStatus(bool IsValid, bool RequiresReauth, IReadOnlyList<string> Scopes);
