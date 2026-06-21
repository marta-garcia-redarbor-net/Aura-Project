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

    [Fact]
    public void WorkItemPorts_ShouldResideInApplicationPortsNamespace()
    {
        var assembly = typeof(Aura.Application.Ports.IConnectorAdapter).Assembly;
        _ = typeof(Aura.Application.Ports.IWorkItemBuffer);
        _ = typeof(Aura.Application.Ports.IWorkItemStore);

        var bufferPortResult = Types
            .InAssembly(assembly)
            .That()
            .HaveName("IWorkItemBuffer")
            .Should()
            .ResideInNamespace("Aura.Application.Ports")
            .GetResult();

        Assert.True(bufferPortResult.IsSuccessful,
            $"IWorkItemBuffer must reside in Aura.Application.Ports: {FormatFailingTypes(bufferPortResult)}");

        var storePortResult = Types
            .InAssembly(assembly)
            .That()
            .HaveName("IWorkItemStore")
            .Should()
            .ResideInNamespace("Aura.Application.Ports")
            .GetResult();

        Assert.True(storePortResult.IsSuccessful,
            $"IWorkItemStore must reside in Aura.Application.Ports: {FormatFailingTypes(storePortResult)}");

        var noInfrastructureDependency = Types
            .InAssembly(assembly)
            .That()
            .HaveName("IWorkItemStore")
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure")
            .GetResult();

        Assert.True(noInfrastructureDependency.IsSuccessful,
            $"IWorkItemStore references Aura.Infrastructure: {FormatFailingTypes(noInfrastructureDependency)}");
    }

    [Fact]
    public void ApplicationAndDomain_ShouldNotDependOnTeamsInfrastructureTypes()
    {
        var appResult = Types
            .InAssembly(typeof(Aura.Application.Ports.IConnectorAdapter).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure.Adapters.Connectors.Teams")
            .GetResult();

        Assert.True(appResult.IsSuccessful,
            $"Application references Teams infrastructure types: {FormatFailingTypes(appResult)}");

        var domainResult = Types
            .InAssembly(typeof(Aura.Domain.WorkItems.WorkItem).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure.Adapters.Connectors.Teams")
            .GetResult();

        Assert.True(domainResult.IsSuccessful,
            $"Domain references Teams infrastructure types: {FormatFailingTypes(domainResult)}");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "none";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
