using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Aura.Infrastructure.Adapters.Identity;

/// <summary>
/// Generates symmetric JWTs with configurable claims for local development.
/// NOT intended for production — guarded by environment checks at the endpoint level.
/// </summary>
public sealed class MockJwtGenerator
{
    private readonly MockJwtOptions _options;

    public MockJwtGenerator(IOptions<MockJwtOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Generates a signed JWT containing the specified user claims.
    /// </summary>
    public string GenerateToken(
        string userId = "mock-user-001",
        string displayName = "Mock User",
        string email = "mock@aura.dev")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(ClaimTypes.Email, email)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
