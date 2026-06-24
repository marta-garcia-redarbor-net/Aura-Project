using Aura.UI.Services;
using Microsoft.Identity.Client;
using NSubstitute;

namespace Aura.UnitTests.UI;

public class MsalTokenAcquisitionServiceTests
{
    [Fact]
    public void MsalTokenAcquisitionService_ShouldImplement_ITokenAcquisitionService()
    {
        // Arrange & Act
        var msalApp = Substitute.For<IPublicClientApplication>();
        var service = new MsalTokenAcquisitionService(msalApp);

        // Assert
        Assert.IsAssignableFrom<ITokenAcquisitionService>(service);
    }

    [Fact]
    public void MsalTokenAcquisitionService_ShouldHaveConstructor_WithIPublicClientApplication()
    {
        // Arrange & Act
        var msalApp = Substitute.For<IPublicClientApplication>();
        var service = new MsalTokenAcquisitionService(msalApp);

        // Assert
        Assert.NotNull(service);
    }
}