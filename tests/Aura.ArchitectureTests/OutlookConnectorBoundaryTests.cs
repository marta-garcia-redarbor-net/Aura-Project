using NetArchTest.Rules;

namespace Aura.ArchitectureTests;

public class OutlookConnectorBoundaryTests
{
    [Fact]
    public void Application_ShouldNotDependOn_OutlookConnectorInfrastructureTypes()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.IConnectorAdapter).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure.Adapters.Connectors.Outlook")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application references Outlook infrastructure types: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_OutlookConnectorInfrastructureTypes()
    {
        var result = Types
            .InAssembly(typeof(Aura.Domain.WorkItems.WorkItem).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure.Adapters.Connectors.Outlook")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain references Outlook infrastructure types: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "none";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
