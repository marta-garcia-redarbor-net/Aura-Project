using NetArchTest.Rules;

namespace Aura.ArchitectureTests;

public class ConnectorExecutionArchitectureTests
{
    [Fact]
    public void ConnectorAdapter_Port_ShouldResideInApplicationPortsNamespace()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.IConnectorAdapter).Assembly)
            .That()
            .HaveName("IConnectorAdapter")
            .Should()
            .ResideInNamespace("Aura.Application.Ports")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"IConnectorAdapter must reside in Aura.Application.Ports: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Application_ShouldNotReference_MicrosoftGraphSdk_FromConnectorExecutionFlow()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.IConnectorAdapter).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Graph")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application references Microsoft.Graph types: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "none";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
