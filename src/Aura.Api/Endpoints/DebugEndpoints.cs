using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Endpoints;

/// <summary>
/// Debug endpoints: manual notification testing and user info inspection.
/// Only registered in Development environment.
/// </summary>
public static partial class DebugEndpoints
{
    public static IEndpointRouteBuilder MapDebugEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/debug");   // No auth — dev-only endpoint

        group.MapGet("/whoami", GetWhoAmIAsync);
        group.MapPost("/fire-notification", PostFireNotificationAsync);

        return endpoints;
    }

    /// <summary>
    /// Returns environment info and instructions for finding your OID.
    /// </summary>
    private static IResult GetWhoAmIAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        return Results.Ok(new
        {
            Message = "Debug endpoint is active. To find your OID, check the browser dev tools " +
                      "when logged into the Aura UI: look for network requests to the API " +
                      "and decode the Bearer JWT at jwt.io — the 'oid' claim is your user ID.",
            HasAuthHeader = authHeader is not null,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    /// <summary>
    /// Fires a test notification directly into the outbox, bypassing ingestion.
    /// </summary>
    private static async Task<IResult> PostFireNotificationAsync(
        INotificationOutboxStore outboxStore,
        ILoggerFactory loggerFactory,
        CancellationToken ct,
        string title = "🧪 Prueba manual desde debug endpoint",
        string source = "Debug",
        double priority = 9.0,
        string triggerRule = "DebugEndpoint",
        string userId = "mock-user-001")
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.Debug");

        var entry = new NotificationOutboxEntry(
            workItemId: Guid.NewGuid(),
            userId: userId,
            sourceType: source,
            title: title,
            priority: priority,
            triggerRule: triggerRule);

        await outboxStore.EnqueueAsync(entry, ct);

        Log.NotificationQueued(logger, entry.Id, userId, title, priority);

        return Results.Ok(new
        {
            Message = "Notification queued. Worker will dispatch via SignalR within ~2s.",
            NotificationId = entry.Id,
            UserId = userId,
            Title = title,
            Priority = priority
        });
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 9001, Level = LogLevel.Information,
            Message = "Debug notification queued: {NotificationId} for user {UserId} — \"{Title}\" (priority {Priority})")]
        public static partial void NotificationQueued(ILogger logger, Guid notificationId, string userId, string title, double priority);
    }
}
