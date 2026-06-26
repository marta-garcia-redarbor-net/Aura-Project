namespace Aura.Infrastructure.Adapters.Identity;

/// <summary>
/// Microsoft Entra ID claim type URIs.
/// Centralized to avoid magic string duplication across identity adapters.
/// </summary>
internal static class EntraIdClaims
{
    /// <summary>Entra ID object identifier — immutable across tenants and email changes.</summary>
    public const string ObjectId = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    /// <summary>Entra ID tenant identifier.</summary>
    public const string TenantId = "http://schemas.microsoft.com/identity/claims/tenantid";
}
