using NetArchTest.Rules;

namespace Aura.ArchitectureTests;

public class TeamsConnectorBoundaryTests
{
    private const string TeamsConnectorNamespace = "Aura.Infrastructure.Adapters.Connectors.Teams";

    [Fact]
    public void TeamsMapper_ShouldNotDependOn_PriorityScoringService()
    {
        var infraAssembly = typeof(Aura.Infrastructure.DependencyInjection).Assembly;

        var result = Types
            .InAssembly(infraAssembly)
            .That()
            .ResideInNamespace(TeamsConnectorNamespace)
            .And()
            .HaveName("TeamsWorkItemMapper")
            .ShouldNot()
            .HaveDependencyOn("Aura.Application.Ports")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"TeamsWorkItemMapper should not depend on Application.Ports (IPriorityScoringService). " +
            $"Failing: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void TeamsConnector_ShouldNotDependOn_InterruptionPolicyService()
    {
        var infraAssembly = typeof(Aura.Infrastructure.DependencyInjection).Assembly;

        var result = Types
            .InAssembly(infraAssembly)
            .That()
            .ResideInNamespace(TeamsConnectorNamespace)
            .ShouldNot()
            .HaveDependencyOn("Aura.Application.Services")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Teams connector should not depend on Application.Services (InterruptionPolicyEngine). " +
            $"Failing: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "none";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
