using System.Diagnostics;
using Aura.Application.Ports;
using Aura.Application.UseCases.IngestionSync;
using Aura.Infrastructure.Adapters.Connectors.Graph;
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
            .RequireAuthorization("RequireEntraId");

        group.MapPost("/now", PostSyncNowAsync);
        group.MapGet("/status", GetSyncStatusAsync);

        return endpoints;
    }

    private static async Task<IResult> PostSyncNowAsync(
        TriggerSyncUseCase useCase,
        ICurrentUserService currentUserService,
        OboTokenService oboTokenService,
        HttpContext httpContext,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.Sync");

        using var activity = ActivitySource.StartActivity("sync.now", ActivityKind.Server);
        activity?.SetTag("sync.endpoint", "/api/sync/now");

        try
        {
            var userOid = currentUserService.GetCurrentUser()?.Oid;
            if (string.IsNullOrWhiteSpace(userOid))
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Missing authenticated oid");
                Log.ClaimsOidMissing(logger);
                return Results.Unauthorized();
            }

            Log.ClaimsOidResolved(logger, userOid);

            // Acquire and cache a Graph token via OBO for the worker.
            // Requires the app registration to have delegated Graph permissions
            // (Mail.Read, Chat.Read, Calendars.Read) and a valid client secret.
            var bearerToken = ExtractBearerToken(httpContext);
            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                Log.OboTokenCacheAttempt(logger, userOid);
                var cached = await oboTokenService.CacheTokenForUserAsync(userOid, bearerToken);
                if (cached)
                {
                    Log.OboTokenCached(logger, userOid);
                }
                else
                {
                    Log.OboTokenFailed(logger, userOid);
                }
            }
            else
            {
                Log.OboTokenMissingBearer(logger, userOid);
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

    private static string? ExtractBearerToken(HttpContext httpContext)
    {
        var auth = httpContext.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        return auth["Bearer ".Length..].Trim();
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
            Message = "Resolved user oid from authenticated claims: {UserOid}")]
        public static partial void ClaimsOidResolved(ILogger logger, string userOid);

        [LoggerMessage(EventId = 6008, Level = LogLevel.Warning,
            Message = "Sync now blocked — authenticated oid claim missing")]
        public static partial void ClaimsOidMissing(ILogger logger);

        [LoggerMessage(EventId = 6009, Level = LogLevel.Warning,
            Message = "Sync now claims validation warning: {Reason}")]
        public static partial void ClaimsValidationWarning(ILogger logger, string reason);

        [LoggerMessage(EventId = 6010, Level = LogLevel.Information,
            Message = "OBO token cached for oid={UserOid}")]
        public static partial void OboTokenCached(ILogger logger, string userOid);

        [LoggerMessage(EventId = 6011, Level = LogLevel.Warning,
            Message = "OBO token acquisition failed for oid={UserOid}")]
        public static partial void OboTokenFailed(ILogger logger, string userOid);

        [LoggerMessage(EventId = 6012, Level = LogLevel.Information,
            Message = "Attempting OBO token acquisition for oid={UserOid}")]
        public static partial void OboTokenCacheAttempt(ILogger logger, string userOid);

        [LoggerMessage(EventId = 6013, Level = LogLevel.Warning,
            Message = "Skipping OBO token acquisition for oid={UserOid}: missing bearer token in request")]
        public static partial void OboTokenMissingBearer(ILogger logger, string userOid);
    }
}
