using NetArchTest.Rules;

namespace Aura.ArchitectureTests;

public class ReviewerIdentityArchitectureTests
{
    [Fact]
    public void Application_ShouldNotDependOn_MicrosoftTeamFoundation()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Mapping.PullRequestMapper).Assembly)
            .That()
            .ResideInNamespace("Aura.Application")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.TeamFoundation")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Aura.Application references Microsoft.TeamFoundation: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_MicrosoftTeamFoundation()
    {
        var result = Types
            .InAssembly(typeof(Aura.Domain.WorkItems.WorkItem).Assembly)
            .That()
            .ResideInNamespace("Aura.Domain")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.TeamFoundation")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Aura.Domain references Microsoft.TeamFoundation: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "none";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
