using System.Diagnostics;
using Aura.Application.Ports;
using Aura.Application.UseCases.IngestionSync;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Endpoints;

/// <summary>
/// Sync endpoints: POST /api/sync/now triggers ingestion, GET /api/sync/status returns per-source state.
/// </summary>
public static partial class SyncEndpoints
{
    private static readonly ActivitySource ActivitySource = new("Aura.Api");

    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/sync")
            .RequireAuthorization();

        group.MapPost("/now", PostSyncNowAsync);
        group.MapGet("/status", GetSyncStatusAsync);

        return endpoints;
    }

    private static async Task<IResult> PostSyncNowAsync(
        TriggerSyncUseCase useCase,
        Microsoft.Identity.Client.IPublicClientApplication? msalApp,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.Sync");

        using var activity = ActivitySource.StartActivity("sync.now", ActivityKind.Server);
        activity?.SetTag("sync.endpoint", "/api/sync/now");

        try
        {
            // Resolve user oid from MSAL token cache for delegated Graph operations
            string? userOid = null;
            if (msalApp is not null)
            {
                try
                {
                    var accounts = await msalApp.GetAccountsAsync();
                    var account = accounts.FirstOrDefault();
                    if (account is not null)
                    {
                        userOid = account.HomeAccountId.ObjectId;
                        Log.UserOidResolved(logger, userOid);
                    }
                    else
                    {
                        Log.NoCachedAccount(logger);
                    }
                }
                catch (Exception ex)
                {
                    Log.MsalCacheReadFailed(logger, ex);
                }
            }

            var result = await useCase.ExecuteAsync(userOid, cancellationToken);
            activity?.SetTag("sync.source_count", result.Results.Count);

            Log.SyncNowCompleted(logger, result.Results.Count);
            return Results.Ok(result);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.SyncNowCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.SyncNowFailed(logger, ex);
            return Results.Problem(title: "Sync request failed", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetSyncStatusAsync(
        ISyncStateStore syncStateStore,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.Sync");

        using var activity = ActivitySource.StartActivity("sync.status.read", ActivityKind.Server);
        activity?.SetTag("sync.endpoint", "/api/sync/status");

        try
        {
            var states = await syncStateStore.GetAllAsync(cancellationToken);
            activity?.SetTag("sync.status.source_count", states.Count);

            Log.SyncStatusReturned(logger, states.Count);
            return Results.Ok(states);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.SyncStatusCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.SyncStatusFailed(logger, ex);
            return Results.Problem(title: "Sync status request failed", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 6001, Level = LogLevel.Information,
            Message = "Sync now completed with {SourceCount} source(s)")]
        public static partial void SyncNowCompleted(ILogger logger, int sourceCount);

        [LoggerMessage(EventId = 6002, Level = LogLevel.Warning,
            Message = "Sync now request was cancelled")]
        public static partial void SyncNowCancelled(ILogger logger);

        [LoggerMessage(EventId = 6003, Level = LogLevel.Error,
            Message = "Sync now request failed")]
        public static partial void SyncNowFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 6004, Level = LogLevel.Information,
            Message = "Sync status returned {SourceCount} source(s)")]
        public static partial void SyncStatusReturned(ILogger logger, int sourceCount);

        [LoggerMessage(EventId = 6005, Level = LogLevel.Warning,
            Message = "Sync status request was cancelled")]
        public static partial void SyncStatusCancelled(ILogger logger);

        [LoggerMessage(EventId = 6006, Level = LogLevel.Error,
            Message = "Sync status request failed")]
        public static partial void SyncStatusFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 6007, Level = LogLevel.Information,
            Message = "Resolved user oid from MSAL cache: {UserOid}")]
        public static partial void UserOidResolved(ILogger logger, string userOid);

        [LoggerMessage(EventId = 6008, Level = LogLevel.Warning,
            Message = "No cached account found in MSAL — sync will run without user identity")]
        public static partial void NoCachedAccount(ILogger logger);

        [LoggerMessage(EventId = 6009, Level = LogLevel.Warning,
            Message = "Failed to read MSAL token cache for sync")]
        public static partial void MsalCacheReadFailed(ILogger logger, Exception exception);
    }
}
