using NetArchTest.Rules;

namespace Aura.ArchitectureTests;

public class TriageAdvisoryArchitectureTests
{
    [Fact]
    public void AdvisoryPorts_MustRemainInApplicationAndBeInterfaces()
    {
        var appAssembly = typeof(Aura.Application.Ports.ILlmDecisionAdvisor).Assembly;

        var result = Types
            .InAssembly(appAssembly)
            .That()
            .ResideInNamespace("Aura.Application.Ports")
            .And()
            .HaveNameMatching("I(LlmDecisionAdvisor|DecisionContextRetriever)")
            .Should()
            .BeInterfaces()
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Expected advisory ports to be interfaces in Aura.Application.Ports. Failing: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void DomainAndApplication_MustNotDependOnQdrantClientSdk()
    {
        var domainAssembly = typeof(Aura.Domain.SemanticIndex.ValueObjects.SemanticChunk).Assembly;
        var applicationAssembly = typeof(Aura.Application.Ports.ILlmDecisionAdvisor).Assembly;

        var domainResult = Types
            .InAssembly(domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Qdrant.Client")
            .GetResult();

        var appResult = Types
            .InAssembly(applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Qdrant.Client")
            .GetResult();

        Assert.True(domainResult.IsSuccessful,
            $"Domain references Qdrant.Client SDK. Failing: {FormatFailingTypes(domainResult)}");
        Assert.True(appResult.IsSuccessful,
            $"Application references Qdrant.Client SDK. Failing: {FormatFailingTypes(appResult)}");
    }

    private static string FormatFailingTypes(TestResult result)
        => result.FailingTypes is null || !result.FailingTypes.Any()
            ? "none"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}
