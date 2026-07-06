using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Application.UseCases.ConnectorExecution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Aura.Workers;

/// <summary>
/// Continuous polling worker that runs the connector execution use case for all registered connectors.
/// Uses lightweight strategy dispatch in the use case and relies on Application checkpoint policy.
/// Resolves user oid from MSAL token cache for delegated Graph operations.
/// </summary>
public sealed partial class ConnectorExecutionWorker : CorrelatedWorkerBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<ConnectorExecutionOptions> _options;
    private readonly ILogger<ConnectorExecutionWorker> _logger;

    public ConnectorExecutionWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<ConnectorExecutionOptions> options,
        ILogger<ConnectorExecutionWorker> logger)
        : base(logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteCorrelatedAsync(string correlationId, CancellationToken stoppingToken)
    {
        var pollingInterval = TimeSpan.FromSeconds(_options.Value.PollingIntervalSeconds);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ExecuteConnectorUseCase>();
            var adapters = scope.ServiceProvider.GetServices<IConnectorAdapter>();

            // Resolve user oid from MSAL token cache (delegated flow).
            // Workers cannot use ICurrentUserService (no HTTP context).
            string? oid = null;
            try
            {
                var msalApp = scope.ServiceProvider.GetRequiredService<IPublicClientApplication>();
                var accounts = await msalApp.GetAccountsAsync();
                var account = accounts.FirstOrDefault();
                if (account is not null)
                {
                    oid = account.HomeAccountId.ObjectId;
                }
            }
            catch (Exception ex)
            {
                Log.TokenCacheReadFailed(_logger, ex);
            }

            foreach (var adapter in adapters)
            {
                if (oid is null)
                {
                    Log.NoCachedUser(_logger, adapter.ConnectorName);
                    continue;
                }

                var identity = new CheckpointIdentity(
                    adapter.ConnectorName,
                    GetSource(adapter.ConnectorName),
                    "default",
                    userOid: oid);

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

        try
        {
            await Task.Delay(pollingInterval, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Exit cleanly
        }
    }

    private static string GetSource(string connectorName) =>
        connectorName switch
        {
            "teams" => "messages",
            "outlook" => "inbox",
            "calendar" => "calendar",
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

        [LoggerMessage(
            EventId = 4206,
            Level = LogLevel.Warning,
            Message = "ConnectorExecutionWorker skipping {Connector} — no cached user identity")]
        public static partial void NoCachedUser(ILogger logger, string connector);

        [LoggerMessage(
            EventId = 4207,
            Level = LogLevel.Warning,
            Message = "ConnectorExecutionWorker failed to read token cache")]
        public static partial void TokenCacheReadFailed(ILogger logger, Exception exception);
    }
}
