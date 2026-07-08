using System.Reflection;

namespace Aura.UnitTests.Persistence;

/// <summary>
/// RED: Tests that verify EF Core packages are available and the AuraDbContext
/// can be constructed with all expected entity configurations.
/// These tests are written BEFORE the production code exists (TDD cycle).
/// </summary>
public class AuraDbContextRegistrationTests
{
    private const string InfrastructureAssemblyName = "Aura.Infrastructure";

    /// <summary>
    /// RED for Task 1.1: Verifies that Microsoft.EntityFrameworkCore is resolvable
    /// from the Infrastructure assembly (fails until packages are added).
    /// </summary>
    [Fact]
    public void InfrastructureAssembly_ReferencesEntityFrameworkCore()
    {
        var infraAssembly = Assembly.Load(InfrastructureAssemblyName);

        var efCoreTypes = infraAssembly.GetReferencedAssemblies()
            .FirstOrDefault(a => a.Name == "Microsoft.EntityFrameworkCore");

        Assert.NotNull(efCoreTypes);
    }

    /// <summary>
    /// RED for Task 1.1: Verifies the EF Core Sqlite provider assembly is loadable.
    /// Required for in-memory SQLite in unit tests and local dev.
    /// </summary>
    [Fact]
    public void EntityFrameworkCoreSqlite_ProviderAssemblyIsLoadable()
    {
        // Microsoft.EntityFrameworkCore.Sqlite provides SqliteDatabaseFacadeExtensions
        // which enables UseSqlite() on DbContext options builder.
        var assembly = Assembly.Load("Microsoft.EntityFrameworkCore.Sqlite");
        Assert.NotNull(assembly);
    }

    /// <summary>
    /// RED for Task 1.2: Verifies that AuraDbContext type exists in Infrastructure
    /// and exposes DbSet properties for all 9 expected tables.
    /// </summary>
    [Fact]
    public void AuraDbContext_HasExpectedDbSets()
    {
        var infraAssembly = Assembly.Load(InfrastructureAssemblyName);
        var dbContextType = infraAssembly.GetType("Aura.Infrastructure.Adapters.Persistence.AuraDbContext");

        Assert.NotNull(dbContextType);

        var expectedDbSetNames = new[]
        {
            "FocusStateOverrides",
            "InterruptionDecisions",
            "AlertRules",
            "Notifications",
            "MeetingAlerts",
            "MorningSummaryEmission",
            "WorkItems",
            "SemanticOutbox",
            "MsalTokenCache"
        };

        foreach (var setName in expectedDbSetNames)
        {
            var property = dbContextType.GetProperty(setName, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(property);
        }
    }

    /// <summary>
    /// RED for Task 1.3: Verifies that all 9 entity type configurations exist
    /// in the expected namespace and implement IEntityTypeConfiguration.
    /// </summary>
    [Fact]
    public void AllEntityConfigurations_ExistAndImplementIEntityTypeConfiguration()
    {
        var infraAssembly = Assembly.Load(InfrastructureAssemblyName);
        var configNamespace = "Aura.Infrastructure.Adapters.Persistence.EntityConfigurations";

        var expectedConfigs = new[]
        {
            "FocusStateOverrideConfiguration",
            "InterruptionDecisionConfiguration",
            "AlertRuleConfiguration",
            "NotificationConfiguration",
            "MeetingAlertConfiguration",
            "MorningSummaryEmissionConfiguration",
            "WorkItemConfiguration",
            "SemanticOutboxConfiguration",
            "MsalTokenCacheConfiguration"
        };

        var iEntityTypeConfigurationType = typeof(Microsoft.EntityFrameworkCore.IEntityTypeConfiguration<>);

        foreach (var configName in expectedConfigs)
        {
            var configType = infraAssembly.GetType($"{configNamespace}.{configName}");
            Assert.NotNull(configType);

            var implementsInterface = configType.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == iEntityTypeConfigurationType);

            Assert.True(implementsInterface,
                $"{configName} should implement IEntityTypeConfiguration<T>");
        }
    }

    /// <summary>
    /// RED for Task 1.4: Verifies IConnectionStringProvider interface exists
    /// with the expected method contract.
    /// </summary>
    [Fact]
    public void IConnectionStringProvider_ExistsWithGetConnectionString()
    {
        // The interface could live in either Infrastructure or Application
        var infraAssembly = Assembly.Load(InfrastructureAssemblyName);
        var applicationAssembly = Assembly.Load("Aura.Application");

        var interfaceType = infraAssembly.GetType("Aura.Infrastructure.IConnectionStringProvider")
                            ?? applicationAssembly.GetType("Aura.Application.Ports.IConnectionStringProvider");

        Assert.NotNull(interfaceType);
        Assert.True(interfaceType.IsInterface);

        var getMethod = interfaceType.GetMethod("GetConnectionString");
        Assert.NotNull(getMethod);
        Assert.Equal(typeof(string), getMethod.ReturnType);

        var parameters = getMethod.GetParameters();
        Assert.Single(parameters);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal("storeName", parameters[0].Name);
    }
}
