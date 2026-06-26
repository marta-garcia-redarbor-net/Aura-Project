using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.ConnectorExecution;
using Aura.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Client;
using NSubstitute;

namespace Aura.UnitTests.Workers;

public class ConnectorExecutionWorkerTests
{
    [Fact]
    public async Task ExecuteAsync_OneShot_ExecutesUseCaseAndStopsApplication()
    {
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(Arg.Any<CheckpointIdentity>(), Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var adapter = new SuccessAdapter();
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            NullLogger<ExecuteConnectorUseCase>.Instance,
            () => DateTimeOffset.Parse("2026-06-19T15:45:00Z"));

        // Mock IPublicClientApplication with one cached account
        var fakeAccount = Substitute.For<IAccount>();
        fakeAccount.HomeAccountId.Returns(new AccountId("oid-worker-1", "oid-worker-1", null));
        var msalApp = Substitute.For<IPublicClientApplication>();
#pragma warning disable CS0618
        msalApp.GetAccountsAsync()
            .Returns(Task.FromResult((IEnumerable<IAccount>)[fakeAccount]));
#pragma warning restore CS0618

        var services = new ServiceCollection();
        services.AddSingleton(useCase);
        services.AddSingleton<IConnectorAdapter>(adapter);
        services.AddSingleton(msalApp);
        await using var provider = services.BuildServiceProvider();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var worker = new ConnectorExecutionWorker(scopeFactory, lifetime, NullLogger<ConnectorExecutionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await worker.StopAsync(CancellationToken.None);

        await checkpointStore.Received(1).GetAsync(Arg.Any<CheckpointIdentity>(), Arg.Any<CancellationToken>());
        lifetime.Received(1).StopApplication();
    }

    [Fact]
    public async Task ExecuteAsync_NoCachedAccounts_SkipsAllAdapters()
    {
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        var adapter = new SuccessAdapter();
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            NullLogger<ExecuteConnectorUseCase>.Instance,
            () => DateTimeOffset.Parse("2026-06-19T15:45:00Z"));

        // Mock IPublicClientApplication with NO cached accounts
        var msalApp = Substitute.For<IPublicClientApplication>();
#pragma warning disable CS0618
        msalApp.GetAccountsAsync()
            .Returns(Task.FromResult(Enumerable.Empty<IAccount>()));
#pragma warning restore CS0618

        var services = new ServiceCollection();
        services.AddSingleton(useCase);
        services.AddSingleton<IConnectorAdapter>(adapter);
        services.AddSingleton(msalApp);
        await using var provider = services.BuildServiceProvider();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var worker = new ConnectorExecutionWorker(scopeFactory, lifetime, NullLogger<ConnectorExecutionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await worker.StopAsync(CancellationToken.None);

        // Adapter should NOT have been called — no cached user, connector skipped
        Assert.Equal(0, adapter.ExecuteCount);
        lifetime.Received(1).StopApplication();
    }

    [Fact]
    public async Task ExecuteAsync_CachedAccount_SetsUserOidOnIdentity()
    {
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        CheckpointIdentity? capturedIdentity = null;
        checkpointStore.GetAsync(Arg.Any<CheckpointIdentity>(), Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var adapter = new CapturingAdapter(id => capturedIdentity = id);
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            NullLogger<ExecuteConnectorUseCase>.Instance,
            () => DateTimeOffset.Parse("2026-06-19T15:45:00Z"));

        var fakeAccount = Substitute.For<IAccount>();
        fakeAccount.HomeAccountId.Returns(new AccountId("oid-worker-42", "oid-worker-42", null));
        var msalApp = Substitute.For<IPublicClientApplication>();
#pragma warning disable CS0618
        msalApp.GetAccountsAsync()
            .Returns(Task.FromResult((IEnumerable<IAccount>)[fakeAccount]));
#pragma warning restore CS0618

        var services = new ServiceCollection();
        services.AddSingleton(useCase);
        services.AddSingleton<IConnectorAdapter>(adapter);
        services.AddSingleton(msalApp);
        await using var provider = services.BuildServiceProvider();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var worker = new ConnectorExecutionWorker(scopeFactory, lifetime, NullLogger<ConnectorExecutionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await worker.StopAsync(CancellationToken.None);

        Assert.NotNull(capturedIdentity);
        Assert.Equal("oid-worker-42", capturedIdentity!.UserOid);
        lifetime.Received(1).StopApplication();
    }

    [Fact]
    public async Task ExecuteAsync_TokenCacheReadFailed_SkipsAllAdapters()
    {
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        var adapter = new SuccessAdapter();
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new[] { adapter },
            NullLogger<ExecuteConnectorUseCase>.Instance,
            () => DateTimeOffset.Parse("2026-06-19T15:45:00Z"));

        // Mock IPublicClientApplication to throw on GetAccountsAsync
        var msalApp = Substitute.For<IPublicClientApplication>();
#pragma warning disable CS0618
        msalApp.GetAccountsAsync()
            .Returns(Task.FromException<IEnumerable<IAccount>>(new InvalidOperationException("Token cache read failed")));
#pragma warning restore CS0618

        var services = new ServiceCollection();
        services.AddSingleton(useCase);
        services.AddSingleton<IConnectorAdapter>(adapter);
        services.AddSingleton(msalApp);
        await using var provider = services.BuildServiceProvider();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var lifetime = Substitute.For<IHostApplicationLifetime>();
        var worker = new ConnectorExecutionWorker(scopeFactory, lifetime, NullLogger<ConnectorExecutionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await worker.StopAsync(CancellationToken.None);

        // Adapter should NOT have been called — token cache failed, oid is null, connector skipped
        Assert.Equal(0, adapter.ExecuteCount);
        lifetime.Received(1).StopApplication();
    }

    private sealed class SuccessAdapter : IConnectorAdapter
    {
        public int ExecuteCount { get; private set; }
        public string ConnectorName => "teams";

        public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
        {
            ExecuteCount++;
            return Task.FromResult(new ConnectorExecutionResult(request.Identity, 1, ConnectorExecutionStatus.Success));
        }
    }

    private sealed class CapturingAdapter : IConnectorAdapter
    {
        private readonly Action<CheckpointIdentity> _onExecute;
        public string ConnectorName => "teams";

        public CapturingAdapter(Action<CheckpointIdentity> onExecute) => _onExecute = onExecute;

        public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
        {
            _onExecute(request.Identity);
            return Task.FromResult(new ConnectorExecutionResult(request.Identity, 1, ConnectorExecutionStatus.Success));
        }
    }
}
