namespace Aura.Infrastructure.Adapters.Identity;

/// <summary>
/// Configuration for the mock JWT identity provider.
/// Bound from the "MockJwt" configuration section.
/// </summary>
public sealed class MockJwtOptions
{
    public const string SectionName = "MockJwt";

    /// <summary>Symmetric signing key (≥32 characters for HMAC-SHA256).</summary>
    public string Key { get; set; } = "aura-mock-dev-key-do-not-use-in-production-32chars!";

    /// <summary>JWT issuer claim.</summary>
    public string Issuer { get; set; } = "aura-dev";

    /// <summary>JWT audience claim.</summary>
    public string Audience { get; set; } = "aura-api";

    /// <summary>Token lifetime in minutes.</summary>
    public int ExpirationMinutes { get; set; } = 15;
}
