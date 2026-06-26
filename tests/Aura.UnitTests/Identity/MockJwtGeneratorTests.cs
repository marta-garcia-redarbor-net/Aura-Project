using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Aura.Infrastructure.Adapters.Identity;

namespace Aura.UnitTests.Identity;

/// <summary>
/// Unit tests for <see cref="MockJwtGenerator"/>.
/// Verifies synthetic oid claim generation.
/// </summary>
public class MockJwtGeneratorTests
{
    private static MockJwtGenerator CreateGenerator() =>
        new(Options.Create(new MockJwtOptions()));

    [Fact]
    public void GenerateToken_DefaultParameters_ContainsOidClaim()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var tokenString = generator.GenerateToken();
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        // Assert
        var oidClaim = token.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier");
        Assert.NotNull(oidClaim);
        Assert.False(string.IsNullOrEmpty(oidClaim.Value));
    }

    [Fact]
    public void GenerateToken_CustomOid_ReturnsExpectedValue()
    {
        // Arrange
        var generator = CreateGenerator();
        const string expectedOid = "custom-oid-123";

        // Act
        var tokenString = generator.GenerateToken(oid: expectedOid);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        // Assert
        var oidClaim = token.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier");
        Assert.NotNull(oidClaim);
        Assert.Equal(expectedOid, oidClaim.Value);
    }

    [Fact]
    public void GenerateToken_WithUserId_DerivesOidFromUserId()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var tokenString = generator.GenerateToken(userId: "user-456");
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        // Assert — when no explicit oid, should default to userId-derived value
        var oidClaim = token.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier");
        Assert.NotNull(oidClaim);
        Assert.Equal("user-456", oidClaim.Value);
    }

    [Fact]
    public void GenerateToken_StillContainsNameIdentifier()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        var tokenString = generator.GenerateToken(userId: "uid-789");
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        // Assert
        var nameIdClaim = token.Claims.FirstOrDefault(c =>
            c.Type == ClaimTypes.NameIdentifier);
        Assert.NotNull(nameIdClaim);
        Assert.Equal("uid-789", nameIdClaim.Value);
    }
}
