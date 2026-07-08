using NetArchTest.Rules;

namespace Aura.ArchitectureTests;

public class PullRequestApiArchitectureTests
{
    [Fact]
    public void PullRequestMapper_ShouldNotReference_Infrastructure()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Mapping.PullRequestMapper).Assembly)
            .That()
            .HaveNameStartingWith("PullRequestMapper")
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"PullRequestMapper references Aura.Infrastructure: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void PullRequestsEndpoints_ShouldNotReference_UI()
    {
        var result = Types
            .InAssembly(typeof(Aura.Api.Endpoints.PullRequestsEndpoints).Assembly)
            .That()
            .HaveNameStartingWith("PullRequestsEndpoints")
            .ShouldNot()
            .HaveDependencyOn("Aura.UI")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"PullRequestsEndpoints references Aura.UI: {FormatFailingTypes(result)}");
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
