using NetArchTest.Rules;

namespace Aura.ArchitectureTests;

public class GraphConnectorArchitectureTests
{
    [Fact]
    public void Application_ShouldNotReference_MicrosoftGraphSdk()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.ISemanticIndexWriter).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Graph")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application references Microsoft.Graph types: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Domain_ShouldNotReference_MicrosoftGraphSdk()
    {
        var result = Types
            .InAssembly(typeof(Aura.Domain.WorkItems.WorkItem).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Graph")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain references Microsoft.Graph types: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Workers_ShouldNotReference_MicrosoftGraphSdk()
    {
        var result = Types
            .InAssembly(typeof(Aura.Workers.ConnectorExecutionWorker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Graph")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Workers references Microsoft.Graph types: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Api_ShouldNotReference_MicrosoftGraphSdk()
    {
        var result = Types
            .InAssembly(typeof(Aura.Api.Endpoints.SyncEndpoints).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Graph")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Api references Microsoft.Graph types: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Ui_ShouldNotReference_MicrosoftGraphSdk()
    {
        var result = Types
            .InAssembly(typeof(Aura.UI.Program).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Graph")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"UI references Microsoft.Graph types: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Application_ShouldNotReference_MicrosoftIdentityClient()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.ISemanticIndexWriter).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Identity.Client")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application references Microsoft.Identity.Client types: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Domain_ShouldNotReference_MicrosoftIdentityClient()
    {
        var result = Types
            .InAssembly(typeof(Aura.Domain.WorkItems.WorkItem).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.Identity.Client")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain references Microsoft.Identity.Client types: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "none";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
