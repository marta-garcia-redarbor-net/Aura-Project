using Aura.UI.Services;
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
        var service = new DevTokenAcquisitionService(logger);

        // Assert
        Assert.IsAssignableFrom<ITokenAcquisitionService>(service);
    }

    [Fact]
    public void DevTokenAcquisitionService_ShouldHaveConstructor_WithILogger()
    {
        // Arrange & Act
        var logger = Substitute.For<ILogger<DevTokenAcquisitionService>>();
        var service = new DevTokenAcquisitionService(logger);

        // Assert
        Assert.NotNull(service);
    }
}