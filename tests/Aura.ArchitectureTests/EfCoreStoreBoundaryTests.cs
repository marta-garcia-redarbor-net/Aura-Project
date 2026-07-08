namespace Aura.ArchitectureTests;

using NetArchTest.Rules;

/// <summary>
/// Proves that EF Core types (DbContext, entity classes, Ef*Store implementations)
/// do not leak outside the Infrastructure layer. Store port interfaces must remain
/// in the Application layer, and no Application or Api type may depend on EF Core.
/// </summary>
public class EfCoreStoreBoundaryTests
{
    private const string ApplicationNamespace = "Aura.Application";
    private const string InfrastructureNamespace = "Aura.Infrastructure";
    private const string ApiNamespace = "Aura.Api";

    [Fact]
    public void Application_ShouldNotReference_EntityFrameworkCore()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.IWorkItemStore).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application references EntityFrameworkCore: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Api_ShouldNotReference_EntityFrameworkCore()
    {
        var result = Types
            .InAssembly(typeof(Aura.Api.Endpoints.DashboardEndpoints).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Api references EntityFrameworkCore: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Application_ShouldNotReference_AuraDbContext()
    {
        var result = Types
            .InAssembly(typeof(Aura.Application.Ports.IWorkItemStore).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure.Persistence")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application references Infrastructure.Persistence: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void Api_ShouldNotReference_AuraDbContext()
    {
        var result = Types
            .InAssembly(typeof(Aura.Api.Endpoints.DashboardEndpoints).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure.Persistence")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Api references Infrastructure.Persistence: {FormatFailingTypes(result)}");
    }

    [Fact]
    public void EfCoreStores_ShouldResideInInfrastructure()
    {
        var infraAssembly = typeof(Aura.Infrastructure.DependencyInjection).Assembly;

        var efStoreTypes = infraAssembly.GetTypes()
            .Where(t => t.Name.StartsWith("Ef", StringComparison.Ordinal)
                        && t.Name.EndsWith("Store", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(efStoreTypes);

        var misplaced = efStoreTypes
            .Where(t => t.Namespace is null || !t.Namespace.StartsWith(InfrastructureNamespace))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(misplaced.Count == 0,
            $"EF Core store types outside Infrastructure: {string.Join(", ", misplaced)}");
    }

    [Fact]
    public void EfCoreStores_ShouldNotBePublic()
    {
        var infraAssembly = typeof(Aura.Infrastructure.DependencyInjection).Assembly;

        var efStoreTypes = infraAssembly.GetTypes()
            .Where(t => t.Name.StartsWith("Ef", StringComparison.Ordinal)
                        && t.Name.EndsWith("Store", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(efStoreTypes);

        var publicStores = efStoreTypes
            .Where(t => t.IsPublic)
            .Select(t => t.FullName)
            .ToList();

        Assert.True(publicStores.Count == 0,
            $"EF Core store types should be internal but are public: {string.Join(", ", publicStores)}");
    }

    [Fact]
    public void StorePortInterfaces_ShouldResideInApplication()
    {
        var appAssembly = typeof(Aura.Application.Ports.IWorkItemStore).Assembly;

        var storeInterfaces = appAssembly.GetTypes()
            .Where(t => t.IsInterface && t.Name.EndsWith("Store", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(storeInterfaces);

        var misplaced = storeInterfaces
            .Where(t => t.Namespace is null || !t.Namespace.StartsWith(ApplicationNamespace))
            .Select(t => $"{t.FullName} (in {t.Namespace})")
            .ToList();

        Assert.True(misplaced.Count == 0,
            $"Store port interfaces outside Application: {string.Join(", ", misplaced)}");
    }

    [Fact]
    public void AuraDbContext_ShouldNotBeReferencedByDomain()
    {
        var result = Types
            .InAssembly(typeof(Aura.Domain.WorkItems.WorkItem).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Aura.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain references Infrastructure: {FormatFailingTypes(result)}");
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.FailingTypes == null || !result.FailingTypes.Any())
            return "none";
        return string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
