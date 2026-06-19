using NetArchTest.Rules;

namespace Aura.ArchitectureTests;

public class IngestionArchitectureTests
{
    [Fact]
    public void IngestionCheckpointStore_Port_ShouldResideInApplicationPortsNamespace()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.IIngestionCheckpointStore).Assembly)
            .That()
            .HaveName("IIngestionCheckpointStore")
            .Should()
            .ResideInNamespace("Aura.Application.Ports")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"IIngestionCheckpointStore must reside in Aura.Application.Ports: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void IngestionCheckpointStore_Port_ShouldNotReferenceInfrastructureOrProviderSdkTypes()
    {
        var assembly = typeof(Aura.Application.Ports.IIngestionCheckpointStore).Assembly;

        var noInfrastructureDependency = Types
            .InAssembly(assembly)
            .That()
            .HaveName("IIngestionCheckpointStore")
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure")
            .GetResult();

        Assert.True(noInfrastructureDependency.IsSuccessful,
            $"IIngestionCheckpointStore references Aura.Infrastructure: {FormatFailingTypes(noInfrastructureDependency)}");

        var noGraphDependency = Types
            .InAssembly(assembly)
            .That()
            .HaveName("IIngestionCheckpointStore")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Graph")
            .GetResult();

        Assert.True(noGraphDependency.IsSuccessful,
            $"IIngestionCheckpointStore references Microsoft.Graph: {FormatFailingTypes(noGraphDependency)}");

        var noQdrantDependency = Types
            .InAssembly(assembly)
            .That()
            .HaveName("IIngestionCheckpointStore")
            .ShouldNot()
            .HaveDependencyOn("Qdrant.Client")
            .GetResult();

        Assert.True(noQdrantDependency.IsSuccessful,
            $"IIngestionCheckpointStore references Qdrant.Client: {FormatFailingTypes(noQdrantDependency)}");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "none";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
