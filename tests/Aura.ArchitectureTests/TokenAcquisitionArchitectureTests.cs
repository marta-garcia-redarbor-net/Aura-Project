using System.Reflection;

namespace Aura.ArchitectureTests;

public class TokenAcquisitionArchitectureTests
{
    private static readonly Assembly UiAssembly = typeof(Aura.UI.Program).Assembly;

    [Fact]
    public void TokenAcquisitionInterface_ShouldExist_InUiServicesNamespace()
    {
        var type = UiAssembly.GetType("Aura.UI.Services.ITokenAcquisitionService");
        Assert.NotNull(type);
        Assert.True(type.IsInterface, "ITokenAcquisitionService should be an interface");
    }

    [Fact]
    public void MsalTokenAcquisitionService_ShouldExist_InUiServicesNamespace()
    {
        var type = UiAssembly.GetType("Aura.UI.Services.MsalTokenAcquisitionService");
        Assert.NotNull(type);
    }

    [Fact]
    public void DevTokenAcquisitionService_ShouldExist_InUiServicesNamespace()
    {
        var type = UiAssembly.GetType("Aura.UI.Services.DevTokenAcquisitionService");
        Assert.NotNull(type);
    }

    [Fact]
    public void TokenAcquisitionInterface_ShouldDefine_AcquireTokenAsync_Method()
    {
        var type = UiAssembly.GetType("Aura.UI.Services.ITokenAcquisitionService");
        Assert.NotNull(type);

        var method = type.GetMethod("AcquireTokenAsync");
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<string>), method.ReturnType);
    }

    [Fact]
    public void TokenAcquisitionServices_ShouldNotResideInInfrastructure()
    {
        var infrastructureTypes = UiAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains("Infrastructure") == true
                && t.Name.EndsWith("TokenAcquisitionService"))
            .ToList();

        Assert.Empty(infrastructureTypes);
    }
}