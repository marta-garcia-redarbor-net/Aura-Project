using Aura.Application;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.Services;
using Aura.Application.UseCases.ConnectorExecution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aura.UnitTests.Application;

public class DependencyInjectionTests
{
    [Fact]
    public void AddAuraApplication_RegistersISemanticChunkExtractor_AsBasicSemanticChunkExtractor()
    {
        var services = new ServiceCollection();

        services.AddAuraApplication();

        using var provider = services.BuildServiceProvider();
        var extractor = provider.GetRequiredService<ISemanticChunkExtractor>();

        Assert.NotNull(extractor);
        Assert.IsType<BasicSemanticChunkExtractor>(extractor);
    }

    [Fact]
    public void AddAuraApplication_RegistersChunkExtractor_AsSingleton()
    {
        var services = new ServiceCollection();

        services.AddAuraApplication();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ISemanticChunkExtractor));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public async Task AddAuraApplication_ResolvesGraphConnectorStatusReader()
    {
        var services = new ServiceCollection();
        services.AddAuraApplication();
        services.AddSingleton<IGraphConnectorSettingsProvider>(
            new StubGraphConnectorSettingsProvider(new GraphConnectorSettings(
                Enabled: true,
                TenantId: "tenant",
                ClientId: "client",
                HasValidCredentialsBlock: true)));
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IGraphConnectorStatusReader>();

        var status = await reader.GetStatusAsync(CancellationToken.None);

        Assert.Equal(GraphConnectorState.ValidConfig, status.State);
    }

    private sealed class StubGraphConnectorSettingsProvider : IGraphConnectorSettingsProvider
    {
        private readonly GraphConnectorSettings _settings;

        public StubGraphConnectorSettingsProvider(GraphConnectorSettings settings)
        {
            _settings = settings;
        }

        public GraphConnectorSettings GetSettings() => _settings;
    }

    [Fact]
    public void AddAuraApplication_RegistersExecuteConnectorUseCase_AsScoped()
    {
        var services = new ServiceCollection();

        services.AddAuraApplication();

        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ExecuteConnectorUseCase));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Scoped, descriptor!.Lifetime);
    }

    [Fact]
    public async Task AddAuraApplication_ResolvesMorningSummaryScheduler()
    {
        var services = new ServiceCollection();
        services.AddAuraApplication();
        services.AddSingleton<IMorningSummarySettingsProvider>(
            new StubMorningSummarySettingsProvider(new MorningSummarySettings("UTC", new TimeOnly(9, 0))));
        services.AddSingleton<IMorningSummaryEmissionStore>(new StubMorningSummaryEmissionStore());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var scheduler = scope.ServiceProvider.GetRequiredService<IMorningSummaryScheduler>();
        var result = await scheduler.ResolveAsync("system", CancellationToken.None);

        Assert.Equal("UTC", result.ResolvedTimezoneId);
    }

    private sealed class StubMorningSummarySettingsProvider : IMorningSummarySettingsProvider
    {
        private readonly MorningSummarySettings _settings;

        public StubMorningSummarySettingsProvider(MorningSummarySettings settings)
        {
            _settings = settings;
        }

        public MorningSummarySettings GetSettings() => _settings;
    }

    private sealed class StubMorningSummaryEmissionStore : IMorningSummaryEmissionStore
    {
        public Task<bool> HasBeenEmittedAsync(string userId, DateOnly localDate, CancellationToken ct)
            => Task.FromResult(false);

        public Task MarkEmittedAsync(string userId, DateOnly localDate, CancellationToken ct)
            => Task.CompletedTask;

        public Task ResetAsync(string userId, DateOnly localDate, CancellationToken ct)
            => Task.CompletedTask;
    }
}
