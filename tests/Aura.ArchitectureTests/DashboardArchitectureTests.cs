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

    [Fact]
    public void UiModels_ShouldNotReference_AuraDomain()
    {
        var result = Types
            .InAssembly(typeof(Aura.UI.Program).Assembly)
            .That()
            .ResideInNamespace("Aura.UI.Models")
            .ShouldNot()
            .HaveDependencyOn("Aura.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"UI model types reference Aura.Domain: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void DashboardEndpointTypes_ShouldNotReference_AuraDomain()
    {
        var result = Types
            .InAssembly(typeof(Aura.Api.Endpoints.DashboardEndpoints).Assembly)
            .That()
            .ResideInNamespace("Aura.Api.Endpoints")
            .And()
            .HaveNameStartingWith("Dashboard")
            .ShouldNot()
            .HaveDependencyOn("Aura.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Dashboard endpoint types reference Aura.Domain: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void PrioritySummaryService_ShouldNotReference_AuraInfrastructure()
    {
        var result = Types
            .InAssembly(typeof(Aura.UI.Services.PrioritySummaryService).Assembly)
            .That()
            .HaveNameStartingWith("PrioritySummary")
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"PrioritySummaryService references Aura.Infrastructure: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void PrioritySummaryService_ShouldNotReference_AuraDomain()
    {
        var result = Types
            .InAssembly(typeof(Aura.UI.Services.PrioritySummaryService).Assembly)
            .That()
            .HaveNameStartingWith("PrioritySummary")
            .ShouldNot()
            .HaveDependencyOn("Aura.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"PrioritySummaryService references Aura.Domain: {FormatFailingTypes(result)}");
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
