using Aura.Api.Services;
using Aura.Application.Demo;
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
        var group = endpoints.MapGroup("/api/demo");

        group.MapGet("/status", (IOptions<DemoModeOptions> opts) =>
            Results.Ok(new { enabled = opts.Value.Enabled }));

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
            IOptions<DemoModeOptions> opts,
            CancellationToken ct) =>
        {
            if (!opts.Value.Enabled) return Results.Problem("Demo mode is disabled", statusCode: 503);
            var userId = GetUserId(http) ?? "demo-user";
            var ownerUserId = GetUserId(http); // null when no auth → items visible to all
            await demoService.LoadMorningSummaryAsync(userId, ct);
            await demoService.LoadEmailsAsync(ct, ownerUserId: ownerUserId);
            await demoService.LoadTeamsMessagesAsync(ct, ownerUserId: ownerUserId);
            await demoService.LoadCalendarEventsAsync(ct);
            await demoService.LoadPriorityAlertsAsync(ct, ownerUserId: ownerUserId);
            await demoService.LoadPullRequestsAsync(ct, ownerUserId: ownerUserId);
            return Results.Ok(new { message = "Demo data load complete — all seed data persisted" });
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
