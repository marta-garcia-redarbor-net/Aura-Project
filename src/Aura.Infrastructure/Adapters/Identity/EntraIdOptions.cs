namespace Aura.Infrastructure.Adapters.Identity;

/// <summary>
/// Configuration for Entra ID OIDC authentication.
/// Bound from the "AzureAd" configuration section.
/// </summary>
public sealed class EntraIdOptions
{
    public const string SectionName = "AzureAd";

    /// <summary>Entra ID application (client) ID.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>Entra ID tenant ID.</summary>
    public string TenantId { get; set; } = "";

    /// <summary>OAuth scopes to request (default: openid, profile, email).</summary>
    public string[] Scopes { get; set; } = ["openid", "profile", "email"];
}
