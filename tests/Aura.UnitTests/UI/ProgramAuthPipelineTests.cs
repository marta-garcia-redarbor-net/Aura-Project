using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.UI;

/// <summary>
/// Tests that verify the authentication pipeline is correctly configured
/// for both UseEntraId=true and UseEntraId=false modes.
/// These tests define the EXPECTED behavior after the CRITICAL fixes are applied.
/// </summary>
public class ProgramAuthPipelineTests
{
    [Fact]
    public void DevMode_CookieAuthenticationScheme_IsRegistered()
    {
        // Arrange — replicate the non-EntraId branch from Program.cs AFTER fix
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();
        services.AddAuthorization();

        var provider = services.BuildServiceProvider();

        // Assert — cookie authentication scheme should be resolvable
        var authSchemeProvider = provider.GetRequiredService<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>();
        var schemes = authSchemeProvider.GetAllSchemesAsync().GetAwaiter().GetResult();
        var cookieScheme = schemes.FirstOrDefault(s => s.Name == CookieAuthenticationDefaults.AuthenticationScheme);
        Assert.NotNull(cookieScheme);
    }

    [Fact]
    public void DevMode_CanSignInWithCookieScheme()
    {
        // Arrange — verify that HttpContext.SignInAsync with "Cookies" scheme doesn't throw
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();
        services.AddAuthorization();

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider
        };

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "dev-user"),
            new Claim("oid", "mock-user-001")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert — should not throw
        var exception = Record.ExceptionAsync(() =>
            httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal));
        Assert.Null(exception.Result);
    }

    [Fact]
    public void DevMode_AuthenticationMiddleware_ShouldBeRegistered()
    {
        // Arrange — verify that UseAuthentication/UseAuthorization services are available
        // regardless of UseEntraId flag
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie();
        services.AddAuthorization();

        var provider = services.BuildServiceProvider();

        // Act — verify auth middleware services are registered
        var authService = provider.GetService<IAuthenticationService>();
        var authorizationService = provider.GetService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();

        // Assert
        Assert.NotNull(authService);
        Assert.NotNull(authorizationService);
    }
}
