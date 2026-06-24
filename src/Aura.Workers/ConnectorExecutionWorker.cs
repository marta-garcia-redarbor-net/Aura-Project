using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.ConnectorExecution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Workers;

/// <summary>
/// One-shot worker that runs the connector execution use case for all registered connectors and stops the host.
/// Uses lightweight strategy dispatch in the use case and relies on Application checkpoint policy.
/// </summary>
public sealed partial class ConnectorExecutionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ConnectorExecutionWorker> _logger;

    public ConnectorExecutionWorker(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime lifetime,
        ILogger<ConnectorExecutionWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(lifetime);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ExecuteConnectorUseCase>();
            var adapters = scope.ServiceProvider.GetServices<IConnectorAdapter>();

            foreach (var adapter in adapters)
            {
                var identity = new CheckpointIdentity(
                    adapter.ConnectorName,
                    GetSource(adapter.ConnectorName),
                    "default");

                Log.WorkerStarted(_logger, identity.Connector, identity.Source, identity.Tenant);

                var result = await useCase.ExecuteAsync(identity, stoppingToken);

                if (result.Status == ConnectorExecutionStatus.Success)
                {
                    Log.WorkerSucceeded(_logger, result.Identity.Connector, result.ItemCount);
                }
                else
                {
                    Log.WorkerFailed(_logger, result.Identity.Connector, result.FailureReason ?? "Unknown failure");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            Log.WorkerCancelled(_logger);
        }
        catch (Exception ex)
        {
            Log.WorkerCrashed(_logger, ex);
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    private static string GetSource(string connectorName) =>
        connectorName switch
        {
            "teams" => "messages",
            "outlook" => "inbox",
            _ => connectorName
        };

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 4201,
            Level = LogLevel.Information,
            Message = "ConnectorExecutionWorker started for {Connector}/{Source}/{Tenant}")]
        public static partial void WorkerStarted(ILogger logger, string connector, string source, string tenant);

        [LoggerMessage(
            EventId = 4202,
            Level = LogLevel.Information,
            Message = "ConnectorExecutionWorker completed for {Connector}. Items={ItemCount}")]
        public static partial void WorkerSucceeded(ILogger logger, string connector, int itemCount);

        [LoggerMessage(
            EventId = 4203,
            Level = LogLevel.Error,
            Message = "ConnectorExecutionWorker failed for {Connector}. Reason={Reason}")]
        public static partial void WorkerFailed(ILogger logger, string connector, string reason);

        [LoggerMessage(
            EventId = 4204,
            Level = LogLevel.Warning,
            Message = "ConnectorExecutionWorker cancelled")]
        public static partial void WorkerCancelled(ILogger logger);

        [LoggerMessage(
            EventId = 4205,
            Level = LogLevel.Error,
            Message = "ConnectorExecutionWorker crashed unexpectedly")]
        public static partial void WorkerCrashed(ILogger logger, Exception exception);
    }
}
