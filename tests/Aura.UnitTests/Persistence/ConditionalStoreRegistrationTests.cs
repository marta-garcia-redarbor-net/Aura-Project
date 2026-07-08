using Aura.Application.Ports;
using Aura.Infrastructure;
using Aura.Infrastructure.Adapters.Calendar;
using Aura.Infrastructure.Adapters.Connectors.Graph;
using Aura.Infrastructure.Adapters.Decisions;
using Aura.Infrastructure.Adapters.FocusState;
using Aura.Infrastructure.Adapters.MorningSummaryScheduling;
using Aura.Infrastructure.Adapters.Notifications;
using Aura.Infrastructure.Adapters.Rules;
using Aura.Infrastructure.Adapters.SemanticOutbox;
using Aura.Infrastructure.Adapters.WorkItems;
using Aura.Infrastructure.Adapters.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aura.UnitTests.Persistence;

/// <summary>
/// Tests for conditional DI registration of EF Core stores vs SQLite stores.
/// Verifies that the config toggle <c>Persistence:Providers:{StoreName}</c>
/// correctly resolves to either the SQLite or EF Core implementation.
/// </summary>
public class ConditionalStoreRegistrationTests
{
    private static (IServiceCollection Services, IConfiguration Config) BuildServices(string providerValue)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Providers:FocusStateOverride"] = providerValue,
                ["Persistence:Providers:InterruptionDecision"] = providerValue,
                ["Persistence:Providers:AlertRule"] = providerValue,
                ["Persistence:Providers:NotificationOutbox"] = providerValue,
                ["Persistence:Providers:MeetingAlert"] = providerValue,
                ["Persistence:Providers:MorningSummaryEmission"] = providerValue,
                ["Persistence:Providers:WorkItem"] = providerValue,
                ["Persistence:Providers:SemanticOutbox"] = providerValue,
                ["Persistence:Providers:MsalTokenCache"] = providerValue,
                ["ConnectionStrings:AuraDb"] = "Data Source=conditional-di-test;Mode=Memory;Cache=Shared"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        return (services, config);
    }

    [Fact]
    public void RegisterEfStores_WhenProviderIsEntityFramework_ResolvesEfImplementations()
    {
        var (services, config) = BuildServices("EntityFramework");
        services.AddAuraDbContext("AuraDb");
        services.RegisterConditionalStores(config);

        using var sp = services.BuildServiceProvider();

        Assert.IsType<EfFocusStateOverrideStore>(sp.GetRequiredService<IFocusStateOverrideStore>());
        Assert.IsType<EfInterruptionDecisionStore>(sp.GetRequiredService<IInterruptionDecisionStore>());
        Assert.IsType<EfAlertRuleStore>(sp.GetRequiredService<IAlertRuleStore>());
        Assert.IsType<EfNotificationOutboxStore>(sp.GetRequiredService<INotificationOutboxStore>());
        Assert.IsType<EfMeetingAlertStore>(sp.GetRequiredService<IMeetingAlertStore>());
        Assert.IsType<EfMorningSummaryEmissionStore>(sp.GetRequiredService<IMorningSummaryEmissionStore>());
        Assert.IsType<EfWorkItemStore>(sp.GetRequiredService<IWorkItemStore>());
        Assert.IsType<EfSemanticOutboxRepository>(sp.GetRequiredService<ISemanticOutboxRepository>());
        Assert.IsType<EfMsalTokenCacheStore>(sp.GetRequiredService<IMsalTokenCacheStore>());
    }

    [Fact]
    public void GlobalProvider_SetsAllStoresToEntityFramework()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "EntityFramework",
                ["ConnectionStrings:AuraDb"] = "Data Source=global-di-test;Mode=Memory;Cache=Shared"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddAuraDbContext("AuraDb");
        services.RegisterConditionalStores(config);

        using var sp = services.BuildServiceProvider();

        Assert.IsType<EfFocusStateOverrideStore>(sp.GetRequiredService<IFocusStateOverrideStore>());
        Assert.IsType<EfInterruptionDecisionStore>(sp.GetRequiredService<IInterruptionDecisionStore>());
        Assert.IsType<EfAlertRuleStore>(sp.GetRequiredService<IAlertRuleStore>());
        Assert.IsType<EfNotificationOutboxStore>(sp.GetRequiredService<INotificationOutboxStore>());
        Assert.IsType<EfMeetingAlertStore>(sp.GetRequiredService<IMeetingAlertStore>());
        Assert.IsType<EfMorningSummaryEmissionStore>(sp.GetRequiredService<IMorningSummaryEmissionStore>());
        Assert.IsType<EfWorkItemStore>(sp.GetRequiredService<IWorkItemStore>());
        Assert.IsType<EfSemanticOutboxRepository>(sp.GetRequiredService<ISemanticOutboxRepository>());
        Assert.IsType<EfMsalTokenCacheStore>(sp.GetRequiredService<IMsalTokenCacheStore>());
    }

    [Fact]
    public void GlobalProvider_CanBeOverriddenByPerStoreConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "EntityFramework",
                ["Persistence:Providers:FocusStateOverride"] = "Sqlite",
                ["ConnectionStrings:AuraDb"] = "Data Source=override-di-test;Mode=Memory;Cache=Shared"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddAuraDbContext("AuraDb");
        services.RegisterConditionalStores(config);

        using var sp = services.BuildServiceProvider();

        // FocusState is overridden to Sqlite, so EF store is NOT registered
        Assert.Null(sp.GetService<IFocusStateOverrideStore>());

        // WorkItem uses the global default (EntityFramework), so EF store IS registered
        Assert.IsType<EfWorkItemStore>(sp.GetRequiredService<IWorkItemStore>());
    }

    [Fact]
    public void RegisterConditionalStores_DefaultsToSqlite_WhenNoConfigKey()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);

        // When no config key is set, the method should not throw and should
        // not register EF stores (SQLite stores are registered elsewhere in DI)
        services.RegisterConditionalStores(config);

        using var sp = services.BuildServiceProvider();

        // EF stores should NOT be registered when provider is not EntityFramework
        Assert.Null(sp.GetService<IFocusStateOverrideStore>());
    }
}
