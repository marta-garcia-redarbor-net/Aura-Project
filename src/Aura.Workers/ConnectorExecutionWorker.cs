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

    private bool _deviceCodeStarted;

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
            IReadOnlyList<IAccount> accounts = [];
            IPublicClientApplication? msalApp = null;
            try
            {
                msalApp = scope.ServiceProvider.GetRequiredService<IPublicClientApplication>();
                accounts = (await msalApp.GetAccountsAsync()).ToArray();
            }
            catch (Exception ex)
            {
                Log.TokenCacheReadFailed(_logger, ex);
            }

            if (accounts.Count == 0)
            {
                foreach (var adapter in adapters)
                {
                    Log.NoCachedUser(_logger, adapter.ConnectorName);
                }

                // One-time device code flow to seed the shared MSAL token cache.
                // The user visits the URL in their browser, authenticates with their
                // Entra ID account, and the tokens are cached in the SQLite cache
                // shared with the API. Subsequent cycles find the account and sync.
                if (!_deviceCodeStarted && msalApp is not null)
                {
                    _deviceCodeStarted = true;
                    _ = Task.Run(() => AcquireTokenViaDeviceCodeAsync(msalApp, stoppingToken), stoppingToken);
                }

                return;
            }

            foreach (var account in accounts)
            {
                var oid = account.HomeAccountId.ObjectId;
                if (string.IsNullOrWhiteSpace(oid))
                {
                    continue;
                }

                foreach (var adapter in adapters)
                {
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

    /// <summary>
    /// Initiates a device code flow to seed the shared MSAL token cache.
    /// Runs once on first cycle when no cached accounts are found.
    /// After successful authentication, tokens are persisted in the SQLite
    /// cache and subsequent worker cycles find the account automatically.
    /// </summary>
    private async Task AcquireTokenViaDeviceCodeAsync(
        IPublicClientApplication msalApp, CancellationToken ct)
    {
        var scopes = new[] { "Mail.Read", "Mail.ReadBasic", "User.Read", "Calendars.Read", "Chat.Read", "openid", "profile", "email" };

        try
        {
            var result = await msalApp.AcquireTokenWithDeviceCode(scopes, deviceCode =>
            {
                Log.DeviceCodeRequired(_logger, deviceCode.VerificationUrl, deviceCode.UserCode, deviceCode.Message);
                return Task.CompletedTask;
            }).ExecuteAsync(ct);

            Log.DeviceCodeSucceeded(_logger, result.Account.HomeAccountId.ObjectId);
        }
        catch (OperationCanceledException)
        {
            Log.DeviceCodeCancelled(_logger);
        }
        catch (Exception ex)
        {
            Log.DeviceCodeFailed(_logger, ex);
            _deviceCodeStarted = false; // Allow retry on next cycle
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

        [LoggerMessage(
            EventId = 4208,
            Level = LogLevel.Information,
            Message = "Device code auth required. Visit {Url} and enter code {Code}")]
        public static partial void DeviceCodeRequired(ILogger logger, string url, string code, string message);

        [LoggerMessage(
            EventId = 4209,
            Level = LogLevel.Information,
            Message = "Device code auth succeeded for oid={Oid}")]
        public static partial void DeviceCodeSucceeded(ILogger logger, string oid);

        [LoggerMessage(
            EventId = 4210,
            Level = LogLevel.Warning,
            Message = "Device code auth cancelled")]
        public static partial void DeviceCodeCancelled(ILogger logger);

        [LoggerMessage(
            EventId = 4211,
            Level = LogLevel.Error,
            Message = "Device code auth failed")]
        public static partial void DeviceCodeFailed(ILogger logger, Exception exception);
    }
}
