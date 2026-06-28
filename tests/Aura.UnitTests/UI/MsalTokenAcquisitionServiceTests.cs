using Aura.UI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Aura.UnitTests.UI;

public class MsalTokenAcquisitionServiceTests
{
    [Fact]
    public void MsalTokenAcquisitionService_ShouldImplement_ITokenAcquisitionService()
    {
        // Arrange
        IHttpContextAccessor httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        // Act
        MsalTokenAcquisitionService service = new(httpContextAccessor);

        // Assert
        Assert.IsAssignableFrom<ITokenAcquisitionService>(service);
    }

    [Fact]
    public async Task AcquireTokenAsync_WithAccessTokenInSession_ReturnsToken()
    {
        // Arrange
        const string expectedToken = "eyJhbGciOiJSUzI1NiJ9.test-payload.test-signature";

        HttpContext httpContext = BuildHttpContextWithToken("access_token", expectedToken);
        IHttpContextAccessor httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        MsalTokenAcquisitionService service = new(httpContextAccessor);

        // Act
        string token = await service.AcquireTokenAsync();

        // Assert — returns the token from session without AcquireTokenInteractive
        Assert.Equal(expectedToken, token);
    }

    [Fact]
    public async Task AcquireTokenAsync_WhenTokenMissing_ThrowsInvalidOperationException()
    {
        // Arrange — no access_token in session
        HttpContext httpContext = BuildHttpContextWithToken("access_token", tokenValue: null);
        IHttpContextAccessor httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        MsalTokenAcquisitionService service = new(httpContextAccessor);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AcquireTokenAsync());
    }

    [Fact]
    public async Task AcquireTokenAsync_WithDifferentToken_ReturnsThatToken()
    {
        // Arrange — triangulation: different token value
        const string differentToken = "different-access-token-value-xyz";

        HttpContext httpContext = BuildHttpContextWithToken("access_token", differentToken);
        IHttpContextAccessor httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        MsalTokenAcquisitionService service = new(httpContextAccessor);

        // Act
        string token = await service.AcquireTokenAsync();

        // Assert — token identity is preserved exactly
        Assert.Equal(differentToken, token);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds an HttpContext where GetTokenAsync("access_token") returns <paramref name="tokenValue"/>.
    /// <see cref="TokenExtensions.GetTokenAsync"/> calls IAuthenticationService.GetTokenAsync under the hood.
    /// </summary>
    private static HttpContext BuildHttpContextWithToken(string tokenName, string? tokenValue)
    {
        IAuthenticationService authService = Substitute.For<IAuthenticationService>();

        AuthenticateResult authResult = tokenValue is not null
            ? AuthenticateResult.Success(
                new AuthenticationTicket(
                    new System.Security.Claims.ClaimsPrincipal(),
                    new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [$".Token.{tokenName}"] = tokenValue
                    }),
                    "Cookies"))
            : AuthenticateResult.NoResult();

        authService
            .AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string?>())
            .Returns(authResult);

        ServiceCollection services = new();
        services.AddSingleton(authService);

        DefaultHttpContext httpContext = new();
        httpContext.RequestServices = services.BuildServiceProvider();

        return httpContext;
    }
}
