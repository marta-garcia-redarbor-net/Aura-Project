using NetArchTest.Rules;

namespace Aura.ArchitectureTests;

public class DashboardArchitectureTests
{
    [Fact]
    public void Application_ShouldNotReference_DashboardInfrastructureAdapters()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.ISystemStatusReader).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure.Adapters.Dashboard")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application references dashboard infrastructure adapters: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Ui_ShouldNotReference_DashboardInfrastructureAdapters()
    {
        var result = Types
            .InAssembly(typeof(Aura.UI.Program).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure.Adapters.Dashboard")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"UI references dashboard infrastructure adapters: {FormatFailingTypes(result)}");
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
