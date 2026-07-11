using Aura.Application.Demo;
using Aura.Application.Ports;
using Aura.Infrastructure;
using Aura.Infrastructure.Adapters.Demo;
using Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;
using Aura.Infrastructure.Adapters.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aura.UnitTests.Demo;

public class DemoModeRegistrationTests
{
    [Fact]
    public void AddDemoMode_RegistersDemoModeOptions_FromConfig()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoMode:Enabled"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddDemoMode(config);

        using var provider = services.BuildServiceProvider();
        var opts = provider.GetRequiredService<IOptions<DemoModeOptions>>().Value;

        Assert.True(opts.Enabled);
    }

    [Fact]
    public void AddDemoMode_WhenEnabled_RegistersFallbackSemanticServices()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoMode:Enabled"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddDemoMode(config);

        using var provider = services.BuildServiceProvider();
        var retriever = provider.GetRequiredService<ISemanticContextRetriever>();
        var writer = provider.GetRequiredService<ISemanticIndexWriter>();

        Assert.IsType<QdrantFallbackSemanticContextRetriever>(retriever);
        Assert.IsType<QdrantFallbackSemanticIndexWriter>(writer);
    }

    [Fact]
    public void AddDemoMode_WhenEnabled_RegistersDemoService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoMode:Enabled"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        // Stub the stores that DemoService depends on
        services.AddSingleton<Aura.Application.Ports.IWorkItemStore>(NSubstitute.Substitute.For<Aura.Application.Ports.IWorkItemStore>());
        services.AddSingleton<Aura.Application.Ports.IMeetingAlertStore>(NSubstitute.Substitute.For<Aura.Application.Ports.IMeetingAlertStore>());
        services.AddSingleton<Aura.Application.Ports.IMorningSummaryEmissionStore>(NSubstitute.Substitute.For<Aura.Application.Ports.IMorningSummaryEmissionStore>());
        services.AddSingleton<Aura.Application.Ports.INotificationOutboxStore>(NSubstitute.Substitute.For<Aura.Application.Ports.INotificationOutboxStore>());
        services.AddSingleton<Aura.Application.Ports.ICalendarEventStore>(NSubstitute.Substitute.For<Aura.Application.Ports.ICalendarEventStore>());
        services.AddSingleton<Aura.Application.Ports.IDashboardRefreshDispatcher>(NSubstitute.Substitute.For<Aura.Application.Ports.IDashboardRefreshDispatcher>());
        services.AddSingleton<Aura.Application.Ports.IInterruptionDecisionStore>(NSubstitute.Substitute.For<Aura.Application.Ports.IInterruptionDecisionStore>());
        services.AddSingleton<Aura.Application.Ports.IInterruptionPolicyEngine>(NSubstitute.Substitute.For<Aura.Application.Ports.IInterruptionPolicyEngine>());
        services.AddDemoMode(config);

        using var provider = services.BuildServiceProvider();
        var demoService = provider.GetRequiredService<DemoService>();

        Assert.NotNull(demoService);
    }

    [Fact]
    public void AddDemoMode_WhenDisabled_DoesNotRegisterFallbackServices()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoMode:Enabled"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddDemoMode(config);

        using var provider = services.BuildServiceProvider();
        var retriever = provider.GetService<ISemanticContextRetriever>();
        var writer = provider.GetService<ISemanticIndexWriter>();

        Assert.Null(retriever);
        Assert.Null(writer);
    }

    [Fact]
    public void AddDemoMode_WhenEnabled_DoesNotOverrideExistingDecisionContextRetriever()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DemoMode:Enabled"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddScoped<IDecisionContextRetriever, QdrantDecisionContextAdapter>();
        services.AddSingleton(NSubstitute.Substitute.For<ISemanticContextRetriever>());
        services.AddLogging();

        services.AddDemoMode(config);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var retriever = scope.ServiceProvider.GetRequiredService<IDecisionContextRetriever>();

        Assert.IsType<QdrantDecisionContextAdapter>(retriever);
    }
}
