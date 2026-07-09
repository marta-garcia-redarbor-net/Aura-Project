using System.Diagnostics;
using Aura.Application.Models;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Endpoints;

/// <summary>
/// Triage endpoints for querying interruption decision history.
/// </summary>
public static partial class TriageEndpoints
{
    private static readonly ActivitySource ActivitySource = new("Aura.Api");

    /// <summary>
    /// Maps triage routes under <c>/api/triage</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapTriageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/triage")
            .RequireAuthorization("RequireEntraId");

        group.MapGet("/decisions", GetDecisionsAsync);

        return endpoints;
    }

    private static async Task<IResult> GetDecisionsAsync(
        IInterruptionDecisionStore decisionStore,
        ILoggerFactory loggerFactory,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.Triage");

        using var activity = ActivitySource.StartActivity("triage.decisions.read", ActivityKind.Server);
        activity?.SetTag("triage.endpoint", "GET /api/triage/decisions");
        activity?.SetTag("triage.page", page);
        activity?.SetTag("triage.page_size", pageSize);

        try
        {
            var result = await decisionStore.QueryAsync(page, pageSize, cancellationToken);

            activity?.SetTag("triage.total_count", result.TotalCount);
            activity?.SetTag("triage.item_count", result.Items.Count);

            Log.DecisionsSucceeded(logger, result.TotalCount, result.Items.Count);

            return Results.Ok(result);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.DecisionsCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.DecisionsFailed(logger, ex);
            return Results.Problem(
                title: "Triage decisions request failed",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 4001, Level = LogLevel.Information,
            Message = "Triage decisions returned {TotalCount} total, {ItemCount} items")]
        public static partial void DecisionsSucceeded(ILogger logger, int totalCount, int itemCount);

        [LoggerMessage(EventId = 4002, Level = LogLevel.Warning,
            Message = "Triage decisions request was cancelled")]
        public static partial void DecisionsCancelled(ILogger logger);

        [LoggerMessage(EventId = 4003, Level = LogLevel.Error,
            Message = "Triage decisions request failed")]
        public static partial void DecisionsFailed(ILogger logger, Exception exception);
    }
}
