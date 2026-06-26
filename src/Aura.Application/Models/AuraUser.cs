namespace Aura.Application.Models;

/// <summary>
/// Domain-neutral representation of the current authenticated user.
/// Pure read DTO consumed by Application use cases — no SDK or identity-provider fields.
/// </summary>
public sealed record AuraUser
{
    /// <summary>Provider-agnostic user identifier.</summary>
    public required string UserId { get; init; }

    /// <summary>Human-readable display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>User email address.</summary>
    public required string Email { get; init; }

    /// <summary>Entra ID object identifier (immutable across tenants and email changes).</summary>
    public string? Oid { get; init; }

    /// <summary>Entra ID tenant identifier.</summary>
    public string? TenantId { get; init; }
}
