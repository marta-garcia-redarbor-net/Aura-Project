namespace Aura.UI.Services;

/// <summary>
/// Represents the result of an authentication attempt.
/// </summary>
public record AuthResult(string Token, bool Success, string? Error);
