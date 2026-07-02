using NetArchTest.Rules;

namespace Aura.ArchitectureTests;

public class OutlookUnreadOnlyArchitectureTests
{
    [Fact]
    public void ExecuteConnectorUseCase_ShouldNotReferenceMicrosoftGraph()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.UseCases.ConnectorExecution.ExecuteConnectorUseCase).Assembly)
            .That()
            .HaveName("ExecuteConnectorUseCase")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Graph")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"ExecuteConnectorUseCase references Microsoft.Graph: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void IWorkItemStore_NewMethods_ShouldNotReferenceInfrastructure()
    {
        var assembly = typeof(Aura.Application.Ports.IWorkItemStore).Assembly;

        var hasNoInfraDependency = Types
            .InAssembly(assembly)
            .That()
            .HaveName("IWorkItemStore")
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure")
            .GetResult();

        Assert.True(hasNoInfraDependency.IsSuccessful,
            $"IWorkItemStore references Aura.Infrastructure: {FormatFailingTypes(hasNoInfraDependency)}");

        var hasNoGraphDependency = Types
            .InAssembly(assembly)
            .That()
            .HaveName("IWorkItemStore")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Graph")
            .GetResult();

        Assert.True(hasNoGraphDependency.IsSuccessful,
            $"IWorkItemStore references Microsoft.Graph: {FormatFailingTypes(hasNoGraphDependency)}");
    }

    [Fact]
    public void Domain_ShouldNotReferenceInfrastructure()
    {
        var result = Types
            .InAssembly(typeof(Aura.Domain.WorkItems.WorkItem).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain references Aura.Infrastructure: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void GraphOutlookSourceProvider_ShouldResideInInfrastructure()
    {
        var infraAssembly = typeof(Aura.Infrastructure.DependencyInjection).Assembly;

        var result = Types
            .InAssembly(infraAssembly)
            .That()
            .HaveName("GraphOutlookSourceProvider")
            .Should()
            .ResideInNamespace("Aura.Infrastructure.Adapters.Connectors.Graph")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"GraphOutlookSourceProvider must reside in Infrastructure: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void SqliteWorkItemStore_Implements_IWorkItemStore()
    {
        var infraAssembly = typeof(Aura.Infrastructure.DependencyInjection).Assembly;

        var result = Types
            .InAssembly(infraAssembly)
            .That()
            .HaveName("SqliteWorkItemStore")
            .Should()
            .HaveDependencyOn("Aura.Application.Ports")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"SqliteWorkItemStore does not depend on Aura.Application.Ports: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void InMemoryWorkItemStore_Implements_IWorkItemStore()
    {
        var infraAssembly = typeof(Aura.Infrastructure.DependencyInjection).Assembly;

        var result = Types
            .InAssembly(infraAssembly)
            .That()
            .HaveName("InMemoryWorkItemStore")
            .Should()
            .HaveDependencyOn("Aura.Application.Ports")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"InMemoryWorkItemStore does not depend on Aura.Application.Ports: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void ApplicationLayer_ShouldNotDependOnInfrastructureConnectors()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.IConnectorAdapter).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure.Adapters.Connectors")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application references Infrastructure connector types: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "none";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
