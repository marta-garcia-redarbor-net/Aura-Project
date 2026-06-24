using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.ConnectorExecution;
using Aura.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
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

        var services = new ServiceCollection();
        services.AddSingleton(useCase);
        services.AddSingleton<IConnectorAdapter>(adapter);
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

    private sealed class SuccessAdapter : IConnectorAdapter
    {
        public string ConnectorName => "teams";

        public Task<ConnectorExecutionResult> ExecuteAsync(ConnectorExecutionRequest request, CancellationToken ct)
            => Task.FromResult(new ConnectorExecutionResult(request.Identity, 1, ConnectorExecutionStatus.Success));
    }
}
