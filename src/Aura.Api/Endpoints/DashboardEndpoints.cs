using System.Diagnostics;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Endpoints;

/// <summary>
/// Dashboard endpoints exposed to the UI host.
/// </summary>
public static partial class DashboardEndpoints
{
    private static readonly ActivitySource ActivitySource = new("Aura.Api");

    /// <summary>
    /// Maps dashboard routes under <c>/api/dashboard</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/dashboard")
            .RequireAuthorization();

        group.MapGet("/initial", GetInitialDashboardAsync);

        return endpoints;
    }

    private static async Task<IResult> GetInitialDashboardAsync(
        IInitialDashboardReader dashboardReader,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.Dashboard");

        using var activity = ActivitySource.StartActivity("dashboard.initial.read", ActivityKind.Server);
        activity?.SetTag("dashboard.endpoint", "/api/dashboard/initial");

        try
        {
            var dashboard = await dashboardReader.GetAsync(cancellationToken);
            activity?.SetTag("dashboard.card_count", dashboard.Cards.Count);
            activity?.SetTag("dashboard.has_cards", dashboard.Cards.Count > 0);

            Log.InitialDashboardSucceeded(logger, dashboard.Cards.Count, dashboard.UserDisplayName);

            return Results.Ok(dashboard);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.InitialDashboardCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.InitialDashboardFailed(logger, ex);
            return Results.Problem(title: "Dashboard request failed", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
            Message = "Initial dashboard returned {CardCount} cards for user {UserDisplayName}")]
        public static partial void InitialDashboardSucceeded(ILogger logger, int cardCount, string userDisplayName);

        [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
            Message = "Initial dashboard request was cancelled")]
        public static partial void InitialDashboardCancelled(ILogger logger);

        [LoggerMessage(EventId = 1003, Level = LogLevel.Error,
            Message = "Initial dashboard request failed")]
        public static partial void InitialDashboardFailed(ILogger logger, Exception exception);
    }
}
