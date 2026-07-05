using Aura.Application.UseCases.Calendar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IPublicClientApplication = Microsoft.Identity.Client.IPublicClientApplication;

namespace Aura.Workers;

public sealed partial class MeetingAlertWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<MeetingAlertWorker> _logger;

    public MeetingAlertWorker(
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime lifetime,
        ILogger<MeetingAlertWorker> logger)
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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var useCase = scope.ServiceProvider.GetRequiredService<CheckAndDispatchMeetingAlertsUseCase>();
                    var msalApp = scope.ServiceProvider.GetService<IPublicClientApplication>();
                    if (msalApp is null)
                    {
                        Log.NoTokenCacheProvider(_logger);
                    }
                    else
                    {
#pragma warning disable CS0618
                        var accounts = await msalApp.GetAccountsAsync();
#pragma warning restore CS0618
                        var userOids = accounts
                            .Select(a => a.HomeAccountId.ObjectId)
                            .Where(oid => !string.IsNullOrWhiteSpace(oid))
                            .Distinct(StringComparer.Ordinal)
                            .ToArray();

                        if (userOids.Length == 0)
                        {
                            Log.NoCachedUsers(_logger);
                        }
                        else
                        {
                            var now = DateTimeOffset.UtcNow;
                            foreach (var userOid in userOids)
                            {
                                await useCase.ExecuteAsync(userOid!, now, stoppingToken);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.PollFailed(_logger, ex);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
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

    private static partial class Log
    {
        [LoggerMessage(EventId = 4401, Level = LogLevel.Warning,
            Message = "MeetingAlertWorker poll failed")]
        public static partial void PollFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 4402, Level = LogLevel.Information,
            Message = "MeetingAlertWorker cancelled")]
        public static partial void WorkerCancelled(ILogger logger);

        [LoggerMessage(EventId = 4403, Level = LogLevel.Error,
            Message = "MeetingAlertWorker crashed unexpectedly")]
        public static partial void WorkerCrashed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 4404, Level = LogLevel.Warning,
            Message = "MeetingAlertWorker skipping poll because MSAL token cache provider is unavailable")]
        public static partial void NoTokenCacheProvider(ILogger logger);

        [LoggerMessage(EventId = 4405, Level = LogLevel.Information,
            Message = "MeetingAlertWorker found no cached users in MSAL token cache")]
        public static partial void NoCachedUsers(ILogger logger);

    }
}
