using NetArchTest.Rules;

namespace Aura.ArchitectureTests;

public class MorningSummaryArchitectureTests
{
    [Fact]
    public void MorningSummaryPorts_ShouldResideInApplicationPortsNamespace()
    {
        var assembly = typeof(Aura.Application.Ports.IConnectorAdapter).Assembly;
        _ = typeof(Aura.Application.Ports.IMorningSummaryComposer);
        _ = typeof(Aura.Application.Ports.IMorningSummaryScheduler);
        _ = typeof(Aura.Application.Ports.IWorkItemReader);
        _ = typeof(Aura.Application.Ports.IMorningSummaryRankingPolicy);

        var composerResult = Types
            .InAssembly(assembly)
            .That()
            .HaveName("IMorningSummaryComposer")
            .Should()
            .ResideInNamespace("Aura.Application.Ports")
            .GetResult();

        Assert.True(composerResult.IsSuccessful,
            $"IMorningSummaryComposer must reside in Aura.Application.Ports: {FormatFailingTypes(composerResult)}");

        var schedulerResult = Types
            .InAssembly(assembly)
            .That()
            .HaveName("IMorningSummaryScheduler")
            .Should()
            .ResideInNamespace("Aura.Application.Ports")
            .GetResult();

        Assert.True(schedulerResult.IsSuccessful,
            $"IMorningSummaryScheduler must reside in Aura.Application.Ports: {FormatFailingTypes(schedulerResult)}");

        var readerResult = Types
            .InAssembly(assembly)
            .That()
            .HaveName("IWorkItemReader")
            .Should()
            .ResideInNamespace("Aura.Application.Ports")
            .GetResult();

        Assert.True(readerResult.IsSuccessful,
            $"IWorkItemReader must reside in Aura.Application.Ports: {FormatFailingTypes(readerResult)}");

        var rankingPolicyPortResult = Types
            .InAssembly(assembly)
            .That()
            .HaveName("IMorningSummaryRankingPolicy")
            .Should()
            .ResideInNamespace("Aura.Application.Ports")
            .GetResult();

        Assert.True(rankingPolicyPortResult.IsSuccessful,
            $"IMorningSummaryRankingPolicy must reside in Aura.Application.Ports: {FormatFailingTypes(rankingPolicyPortResult)}");
    }

    [Fact]
    public void MorningSummaryPorts_ShouldNotDependOnInfrastructureUiOrProviderSdks()
    {
        var assembly = typeof(Aura.Application.Ports.IConnectorAdapter).Assembly;
        _ = typeof(Aura.Application.Ports.IMorningSummaryComposer);
        _ = typeof(Aura.Application.Ports.IMorningSummaryScheduler);
        _ = typeof(Aura.Application.Ports.IWorkItemReader);
        _ = typeof(Aura.Application.Ports.IMorningSummaryRankingPolicy);

        var noInfrastructureDependency = Types
            .InAssembly(assembly)
            .That()
            .HaveNameStartingWith("IMorningSummary")
            .Or()
            .HaveName("IWorkItemReader")
            .Or()
            .HaveName("IMorningSummaryRankingPolicy")
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure")
            .GetResult();

        Assert.True(noInfrastructureDependency.IsSuccessful,
            $"Morning Summary ports reference Aura.Infrastructure: {FormatFailingTypes(noInfrastructureDependency)}");

        var noGraphDependency = Types
            .InAssembly(assembly)
            .That()
            .HaveNameStartingWith("IMorningSummary")
            .Or()
            .HaveName("IWorkItemReader")
            .Or()
            .HaveName("IMorningSummaryRankingPolicy")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Graph")
            .GetResult();

        Assert.True(noGraphDependency.IsSuccessful,
            $"Morning Summary ports reference Microsoft.Graph: {FormatFailingTypes(noGraphDependency)}");

        var noQdrantDependency = Types
            .InAssembly(assembly)
            .That()
            .HaveNameStartingWith("IMorningSummary")
            .Or()
            .HaveName("IWorkItemReader")
            .Or()
            .HaveName("IMorningSummaryRankingPolicy")
            .ShouldNot()
            .HaveDependencyOn("Qdrant.Client")
            .GetResult();

        Assert.True(noQdrantDependency.IsSuccessful,
            $"Morning Summary ports reference Qdrant.Client: {FormatFailingTypes(noQdrantDependency)}");

        var noUiDependency = Types
            .InAssembly(assembly)
            .That()
            .HaveNameStartingWith("IMorningSummary")
            .Or()
            .HaveName("IWorkItemReader")
            .Or()
            .HaveName("IMorningSummaryRankingPolicy")
            .ShouldNot()
            .HaveDependencyOn("Aura.UI")
            .GetResult();

        Assert.True(noUiDependency.IsSuccessful,
            $"Morning Summary ports reference Aura.UI: {FormatFailingTypes(noUiDependency)}");
    }

    [Fact]
    public void MorningSummaryRankingPolicy_ShouldResideInApplicationUseCasesNamespace()
    {
        var applicationAssembly = typeof(Aura.Application.Ports.IConnectorAdapter).Assembly;
        _ = typeof(Aura.Application.UseCases.MorningSummary.MorningSummaryRankingPolicy);

        var residesInApplicationResult = Types
            .InAssembly(applicationAssembly)
            .That()
            .HaveName("MorningSummaryRankingPolicy")
            .Should()
            .ResideInNamespace("Aura.Application.UseCases.MorningSummary")
            .GetResult();

        Assert.True(residesInApplicationResult.IsSuccessful,
            $"MorningSummaryRankingPolicy must reside in Aura.Application.UseCases.MorningSummary: {FormatFailingTypes(residesInApplicationResult)}");
    }

    [Fact]
    public void MorningSummaryRankingPath_ShouldNotReferenceAiPrioritizationPortsOrImplementations()
    {
        var applicationAssembly = typeof(Aura.Application.Ports.IConnectorAdapter).Assembly;
        _ = typeof(Aura.Application.UseCases.MorningSummary.MorningSummaryRankingPolicy);
        _ = typeof(Aura.Application.Ports.IMorningSummaryRankingPolicy);

        var noAiPortDependency = Types
            .InAssembly(applicationAssembly)
            .That()
            .ResideInNamespace("Aura.Application.UseCases.MorningSummary")
            .Or()
            .HaveName("IMorningSummaryRankingPolicy")
            .ShouldNot()
            .HaveDependencyOn("IAiPrioritizationSuggestionProvider")
            .GetResult();

        Assert.True(noAiPortDependency.IsSuccessful,
            $"Morning Summary ranking path references IAiPrioritizationSuggestionProvider: {FormatFailingTypes(noAiPortDependency)}");

        var noAiImplementationDependency = Types
            .InAssembly(applicationAssembly)
            .That()
            .ResideInNamespace("Aura.Application.UseCases.MorningSummary")
            .Or()
            .HaveName("IMorningSummaryRankingPolicy")
            .ShouldNot()
            .HaveDependencyOn("AiPrioritization")
            .GetResult();

        Assert.True(noAiImplementationDependency.IsSuccessful,
            $"Morning Summary ranking path references AI prioritization implementation namespace: {FormatFailingTypes(noAiImplementationDependency)}");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
        {
            return "none";
        }

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
