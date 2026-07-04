using System.Security.Claims;
using Aura.UI.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.UnitTests.UI;

public class DevTokenAcquisitionServiceTests
{
    [Fact]
    public void DevTokenAcquisitionService_ShouldImplement_ITokenAcquisitionService()
    {
        // Arrange & Act
        var logger = Substitute.For<ILogger<DevTokenAcquisitionService>>();
        var authStateProvider = CreateAuthStateProvider();
        var service = new DevTokenAcquisitionService(logger, authStateProvider);

        // Assert
        Assert.IsAssignableFrom<ITokenAcquisitionService>(service);
    }

    [Fact]
    public void DevTokenAcquisitionService_ShouldHaveConstructor_WithILogger()
    {
        // Arrange & Act
        var logger = Substitute.For<ILogger<DevTokenAcquisitionService>>();
        var authStateProvider = CreateAuthStateProvider();
        var service = new DevTokenAcquisitionService(logger, authStateProvider);

        // Assert
        Assert.NotNull(service);
    }

    private static AuthenticationStateProvider CreateAuthStateProvider()
    {
        var authStateProvider = Substitute.For<AuthenticationStateProvider>();
        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        authStateProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        return authStateProvider;
    }
}
