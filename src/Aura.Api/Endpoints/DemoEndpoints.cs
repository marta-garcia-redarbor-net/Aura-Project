using Aura.Api.Services;
using Aura.Application.Demo;
using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Aura.Infrastructure.Adapters.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Aura.Api.Endpoints;

/// <summary>
/// Demo data-loading endpoints. Only functional when DemoMode__Enabled=true.
/// </summary>
public static class DemoEndpoints
{
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/demo")
            .RequireAuthorization("DemoOnly");

        // Status is public (just checks config flag)
        group.MapGet("/status", (IOptions<DemoModeOptions> opts) =>
            Results.Ok(new { enabled = opts.Value.Enabled })).AllowAnonymous();

        group.MapPost("/morning-summary", async (
            DemoService demoService,
            IOptions<DemoModeOptions> opts,
            CancellationToken ct) =>
        {
            if (!opts.Value.Enabled) return Results.Problem("Demo mode is disabled", statusCode: 503);
            var result = await demoService.LoadMorningSummaryAsync("demo-user", ct);
            return Results.Ok(new { message = result });
        });

        group.MapPost("/email", async (
            HttpContext http,
            DemoService demoService,
            IOptions<DemoModeOptions> opts,
            CancellationToken ct) =>
        {
            if (!opts.Value.Enabled) return Results.Problem("Demo mode is disabled", statusCode: 503);
            var userId = GetUserId(http);
            var result = await demoService.LoadEmailsAsync(ct, ownerUserId: userId);
            return Results.Ok(new { message = result });
        });

        group.MapPost("/teams", async (
            HttpContext http,
            DemoService demoService,
            IOptions<DemoModeOptions> opts,
            CancellationToken ct) =>
        {
            if (!opts.Value.Enabled) return Results.Problem("Demo mode is disabled", statusCode: 503);
            var userId = GetUserId(http);
            var result = await demoService.LoadTeamsMessagesAsync(ct, ownerUserId: userId);
            return Results.Ok(new { message = result });
        });

        group.MapPost("/calendar", async (
            DemoService demoService,
            IOptions<DemoModeOptions> opts,
            CancellationToken ct) =>
        {
            if (!opts.Value.Enabled) return Results.Problem("Demo mode is disabled", statusCode: 503);
            var result = await demoService.LoadCalendarEventsAsync(ct);
            return Results.Ok(new { message = result });
        });

        group.MapPost("/priority-alert", async (
            HttpContext http,
            DemoService demoService,
            IOptions<DemoModeOptions> opts,
            CancellationToken ct) =>
        {
            if (!opts.Value.Enabled) return Results.Problem("Demo mode is disabled", statusCode: 503);
            var userId = GetUserId(http);
            var result = await demoService.LoadPriorityAlertsAsync(ct, ownerUserId: userId);
            return Results.Ok(new { message = result });
        });

        group.MapPost("/pull-request", async (
            HttpContext http,
            DemoService demoService,
            IOptions<DemoModeOptions> opts,
            CancellationToken ct) =>
        {
            if (!opts.Value.Enabled) return Results.Problem("Demo mode is disabled", statusCode: 503);
            var userId = GetUserId(http);
            var result = await demoService.LoadPullRequestsAsync(ct, ownerUserId: userId);
            return Results.Ok(new { message = result });
        });

        group.MapPost("/all", async (
            HttpContext http,
            DemoService demoService,
            IMeetingAlertDispatcher meetingAlertDispatcher,
            IMeetingAlertStore meetingAlertStore,
            IOptions<DemoModeOptions> opts,
            CancellationToken ct) =>
        {
            if (!opts.Value.Enabled) return Results.Problem("Demo mode is disabled", statusCode: 503);
            var userId = GetUserId(http) ?? "demo-user";
            var ownerUserId = GetUserId(http); // null when no auth → items visible to all
            var result = await demoService.LoadAllAsync(userId, ct);

            // Create and dispatch a meeting alert for the demo calendar event
            var meetingStart = DateTimeOffset.UtcNow.AddMinutes(50);
            var alert = new MeetingAlert(
                EventId: $"demo-cal-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}",
                Title: "Sprint Planning — Sprint 12",
                Trigger: MeetingAlertTrigger.SixtyMinutes,
                StartsAtUtc: meetingStart,
                JoinUrl: "https://teams.microsoft.com/l/meetup-join/demo-cal",
                UserId: userId,
                HasBeenSent: true);
            await meetingAlertStore.MarkSentAsync(alert, ct);
            await meetingAlertDispatcher.DispatchAsync(alert, ct);

            return Results.Ok(new { message = result });
        });

        group.MapPost("/start-simulation", (
            HttpContext http,
            DemoSimulationService sim,
            IOptions<DemoModeOptions> opts,
            string? userId = null) =>
        {
            if (!opts.Value.Enabled) return Results.Problem("Demo mode is disabled", statusCode: 503);
            // Priority: query param > auth context > null (visible to all)
            var effectiveUserId = userId
                ?? http.User.FindFirst("oid")?.Value
                ?? http.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            sim.Start(effectiveUserId);
            return Results.Ok(new { message = $"Simulation started — data arriving gradually" });
        });

        group.MapPost("/reset", async (
            DemoService demoService,
            IOptions<DemoModeOptions> opts,
            CancellationToken ct) =>
        {
            if (!opts.Value.Enabled) return Results.Problem("Demo mode is disabled", statusCode: 503);
            var result = await demoService.DeleteDemoDataAsync(ct);
            return Results.Ok(new { message = result });
        });

        return endpoints;
    }

    /// <summary>
    /// Returns the authenticated user's ID, or null when called without auth.
    /// null OwnerUserId means the item is visible to all users (dashboard-compatible).
    /// </summary>
    private static string? GetUserId(HttpContext http)
    {
        return http.User.FindFirst("oid")?.Value
               ?? http.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        // No fallback — null means "visible to everyone"
    }
}
