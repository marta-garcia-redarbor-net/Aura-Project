using System.Security.Claims;
using Aura.Infrastructure.Adapters.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.UnitTests.Identity;

/// <summary>
/// Unit tests for <see cref="HttpContextCurrentUserService"/>.
/// Validates ClaimsPrincipal → AuraUser mapping and unauthenticated scenarios.
/// </summary>
public class HttpContextCurrentUserServiceTests
{
    [Fact]
    public void GetCurrentUser_AuthenticatedWithAllClaims_ReturnsAuraUser()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var service = new HttpContextCurrentUserService(accessor, Substitute.For<ILogger<HttpContextCurrentUserService>>());

        // Act
        var user = service.GetCurrentUser();

        // Assert
        Assert.NotNull(user);
        Assert.Equal("user-123", user.UserId);
        Assert.Equal("Test User", user.DisplayName);
        Assert.Equal("test@example.com", user.Email);
    }

    [Fact]
    public void GetCurrentUser_Unauthenticated_ReturnsNull()
    {
        // Arrange — no authentication type = unauthenticated
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var service = new HttpContextCurrentUserService(accessor, Substitute.For<ILogger<HttpContextCurrentUserService>>());

        // Act
        var user = service.GetCurrentUser();

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public void GetCurrentUser_NoHttpContext_ReturnsNull()
    {
        // Arrange
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var service = new HttpContextCurrentUserService(accessor, Substitute.For<ILogger<HttpContextCurrentUserService>>());

        // Act
        var user = service.GetCurrentUser();

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public void GetCurrentUser_MissingNameIdentifier_ReturnsNull()
    {
        // Arrange — authenticated but no NameIdentifier claim
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "No ID User"),
            new Claim(ClaimTypes.Email, "noid@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var service = new HttpContextCurrentUserService(accessor, Substitute.For<ILogger<HttpContextCurrentUserService>>());

        // Act
        var user = service.GetCurrentUser();

        // Assert
        Assert.Null(user);
    }

    [Fact]
    public void GetCurrentUser_MissingOptionalClaims_ReturnsFallbackValues()
    {
        // Arrange — only NameIdentifier, no Name or Email
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-456")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var service = new HttpContextCurrentUserService(accessor, Substitute.For<ILogger<HttpContextCurrentUserService>>());

        // Act
        var user = service.GetCurrentUser();

        // Assert
        Assert.NotNull(user);
        Assert.Equal("user-456", user.UserId);
        Assert.Equal("", user.DisplayName);
        Assert.Equal("", user.Email);
    }

    [Fact]
    public void GetCurrentUser_WithOidClaim_OidPopulated()
    {
        // Arrange — JWT with oid claim
        const string oidClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-789"),
            new Claim(ClaimTypes.Name, "OID User"),
            new Claim(ClaimTypes.Email, "oid@test.com"),
            new Claim(oidClaimType, "entra-oid-abc-123")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var service = new HttpContextCurrentUserService(accessor, Substitute.For<ILogger<HttpContextCurrentUserService>>());

        // Act
        var user = service.GetCurrentUser();

        // Assert
        Assert.NotNull(user);
        Assert.Equal("entra-oid-abc-123", user.Oid);
        Assert.Equal("entra-oid-abc-123", user.UserId);
    }

    [Fact]
    public void GetCurrentUser_WithOidAndTidClaims_BothPopulated()
    {
        // Arrange
        const string oidClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-999"),
            new Claim(oidClaimType, "oid-xyz"),
            new Claim("http://schemas.microsoft.com/identity/claims/tenantid", "tenant-abc")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var service = new HttpContextCurrentUserService(accessor, Substitute.For<ILogger<HttpContextCurrentUserService>>());

        // Act
        var user = service.GetCurrentUser();

        // Assert
        Assert.NotNull(user);
        Assert.Equal("oid-xyz", user.Oid);
        Assert.Equal("tenant-abc", user.TenantId);
    }

    [Fact]
    public void GetCurrentUser_FallsBackToPreferredUsername()
    {
        // Arrange — only NameIdentifier and preferred_username (real access_token scenario)
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-access"),
            new Claim("preferred_username", "marta@contoso.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var service = new HttpContextCurrentUserService(accessor, Substitute.For<ILogger<HttpContextCurrentUserService>>());

        // Act
        var user = service.GetCurrentUser();

        // Assert
        Assert.NotNull(user);
        Assert.Equal("marta@contoso.com", user.DisplayName);
        Assert.Equal("marta@contoso.com", user.Email);
    }

    [Fact]
    public void GetCurrentUser_WithoutOidClaim_OidIsNull()
    {
        // Arrange — mock token without oid claim
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "mock-user"),
            new Claim(ClaimTypes.Name, "Mock")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var service = new HttpContextCurrentUserService(accessor, Substitute.For<ILogger<HttpContextCurrentUserService>>());

        // Act
        var user = service.GetCurrentUser();

        // Assert
        Assert.NotNull(user);
        Assert.Null(user.Oid);
        Assert.Null(user.TenantId);
        Assert.Equal("mock-user", user.UserId);
    }
}
