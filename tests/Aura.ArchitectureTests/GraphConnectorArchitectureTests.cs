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

    [Fact]
    public void IngestionSync_ShouldNotReference_DomainCalendar()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.UseCases.IngestionSync.TriggerSyncUseCase).Assembly)
            .That()
            .ResideInNamespaceContaining("Aura.Application.UseCases.IngestionSync")
            .ShouldNot()
            .HaveDependencyOn("Aura.Domain.Calendar")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"IngestionSync references Aura.Domain.Calendar: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void WorkItemStore_ShouldNotStore_CalendarEvent()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.IWorkItemStore).Assembly)
            .That()
            .ImplementInterface(typeof(Aura.Application.Ports.IWorkItemStore))
            .ShouldNot()
            .HaveDependencyOn("Aura.Domain.Calendar.CalendarEvent")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"WorkItemStore references CalendarEvent: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "none";

        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
