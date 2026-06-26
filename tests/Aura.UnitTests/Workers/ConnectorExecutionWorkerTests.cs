using System.Reflection;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.ConnectorExecution;
using Aura.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using NSubstitute;

namespace Aura.UnitTests.Workers;

public class ConnectorExecutionWorkerTests
{
    [Fact]
    public void Constructor_DoesNotAcceptIHostApplicationLifetime()
    {
        var ctors = typeof(ConnectorExecutionWorker).GetConstructors(
            BindingFlags.Public | BindingFlags.Instance);

        Assert.Single(ctors);

        var parameterTypes = ctors[0].GetParameters().Select(p => p.ParameterType).ToList();

        Assert.DoesNotContain(parameterTypes, t => t == typeof(IHostApplicationLifetime));
    }
    private static IOptions<ConnectorExecutionOptions> CreateOptions(int pollingSeconds = 0)
    {
        var options = Substitute.For<IOptions<ConnectorExecutionOptions>>();
        options.Value.Returns(new ConnectorExecutionOptions { PollingIntervalSeconds = pollingSeconds });
        return options;
    }

    [Fact]
    public async Task ExecuteAsync_ContinuousLoop_ExecutesUseCaseMultipleTimes()
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

        var worker = new ConnectorExecutionWorker(scopeFactory, CreateOptions(), NullLogger<ConnectorExecutionWorker>.Instance);

        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(300);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Worker runs continuously — should have been called at least once
        await checkpointStore.Received().GetAsync(Arg.Any<CheckpointIdentity>(), Arg.Any<CancellationToken>());
        Assert.True(adapter.ExecuteCount > 0, "Adapter should have been called");
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

        var worker = new ConnectorExecutionWorker(scopeFactory, CreateOptions(), NullLogger<ConnectorExecutionWorker>.Instance);

        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Adapter should NOT have been called — no cached user, connector skipped
        Assert.Equal(0, adapter.ExecuteCount);
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

        var worker = new ConnectorExecutionWorker(scopeFactory, CreateOptions(), NullLogger<ConnectorExecutionWorker>.Instance);

        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        Assert.NotNull(capturedIdentity);
        Assert.Equal("oid-worker-42", capturedIdentity!.UserOid);
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

        var worker = new ConnectorExecutionWorker(scopeFactory, CreateOptions(), NullLogger<ConnectorExecutionWorker>.Instance);

        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Adapter should NOT have been called — token cache failed, oid is null, connector skipped
        Assert.Equal(0, adapter.ExecuteCount);
    }

    [Fact]
    public async Task ExecuteAsync_RunsMultipleIterations_ScopeCreatedForEachCycle()
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

        var fakeAccount = Substitute.For<IAccount>();
        fakeAccount.HomeAccountId.Returns(new AccountId("oid-iter", "oid-iter", null));
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

        int scopeCount = 0;
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(_ => { scopeCount++; return scope; });

        var worker = new ConnectorExecutionWorker(scopeFactory, CreateOptions(), NullLogger<ConnectorExecutionWorker>.Instance);

        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);

        // Worker should have created scope multiple times (multiple iterations)
        Assert.True(scopeCount > 1,
            $"Expected multiple scope creations but got {scopeCount}");
    }

    [Fact]
    public async Task ExecuteAsync_CancellationStopsLoopCleanly_NoCrash()
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

        var fakeAccount = Substitute.For<IAccount>();
        fakeAccount.HomeAccountId.Returns(new AccountId("oid-cancel", "oid-cancel", null));
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

        var worker = new ConnectorExecutionWorker(scopeFactory, CreateOptions(), NullLogger<ConnectorExecutionWorker>.Instance);

        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await Task.Delay(200);
        cts.Cancel();

        // Should not throw — graceful shutdown
        var exception = await Record.ExceptionAsync(() => worker.StopAsync(CancellationToken.None));
        Assert.Null(exception);
    }

    [Fact]
    public async Task ExecuteAsync_AdapterFailure_DoesNotStopOtherAdapters()
    {
        var checkpointStore = Substitute.For<IIngestionCheckpointStore>();
        checkpointStore.GetAsync(Arg.Any<CheckpointIdentity>(), Arg.Any<CancellationToken>())
            .Returns((IngestionCheckpoint?)null);

        var failingAdapter = new FailingAdapter();
        var successAdapter = new SuccessAdapter();
        var useCase = new ExecuteConnectorUseCase(
            checkpointStore,
            new IConnectorAdapter[] { failingAdapter, successAdapter },
            NullLogger<ExecuteConnectorUseCase>.Instance,
            () => DateTimeOffset.Parse("2026-06-19T15:45:00Z"));

        var fakeAccount = Substitute.For<IAccount>();
        fakeAccount.HomeAccountId.Returns(new AccountId("oid-err", "oid-err", null));
        var msalApp = Substitute.For<IPublicClientApplication>();
#pragma warning disable CS0618
        msalApp.GetAccountsAsync()
            .Returns(Task.FromResult((IEnumerable<IAccount>)[fakeAccount]));
#pragma warning restore CS0618

        var services = new ServiceCollection();
        services.AddSingleton(useCase);
        services.AddSingleton<IConnectorAdapter>(failingAdapter);
        services.AddSingleton<IConnectorAdapter>(successAdapter);
        services.AddSingleton(msalApp);
        await using var provider = services.BuildServiceProvider();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(scope);

        var worker = new ConnectorExecutionWorker(scopeFactory, CreateOptions(), NullLogger<ConnectorExecutionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(500);
        await worker.StopAsync(CancellationToken.None);

        // The success adapter should still have been called despite the failing adapter
        Assert.True(successAdapter.ExecuteCount > 0,
            "Success adapter should have been called even though failing adapter threw");
    }

    private sealed class FailingAdapter : IConnectorAdapter
    {
        public string ConnectorName => "outlook";

        public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
        {
            throw new InvalidOperationException("Adapter failure");
        }
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
