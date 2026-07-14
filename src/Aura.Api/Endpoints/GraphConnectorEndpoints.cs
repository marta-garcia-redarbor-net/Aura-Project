using System.Diagnostics;
using Aura.Application.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Endpoints;

public static partial class GraphConnectorEndpoints
{
    private static readonly ActivitySource ActivitySource = new("Aura.Api");

    public static IEndpointRouteBuilder MapGraphConnectorEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/connectors/graph")
            .RequireAuthorization("RequireEntraOrDemo");

        group.MapGet("/status", GetStatusAsync);

        return endpoints;
    }

    private static async Task<IResult> GetStatusAsync(
        IGraphConnectorStatusReader statusReader,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.GraphConnector");

        using var activity = ActivitySource.StartActivity("graph.connector.status.read", ActivityKind.Server);
        activity?.SetTag("graph.connector.endpoint", "/api/connectors/graph/status");

        try
        {
            var status = await statusReader.GetStatusAsync(cancellationToken);
            activity?.SetTag("graph.connector.state", status.State.ToString());

            Log.StatusReturned(logger, status.State);
            return Results.Ok(status);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.StatusRequestCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.StatusRequestFailed(logger, ex);
            return Results.Problem(
                title: "Graph connector status request failed",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 2102,
            Level = LogLevel.Information,
            Message = "Graph connector status endpoint returned {State}")]
        public static partial void StatusReturned(ILogger logger, Aura.Application.Models.GraphConnectorState state);

        [LoggerMessage(
            EventId = 2103,
            Level = LogLevel.Warning,
            Message = "Graph connector status request was cancelled")]
        public static partial void StatusRequestCancelled(ILogger logger);

        [LoggerMessage(
            EventId = 2104,
            Level = LogLevel.Error,
            Message = "Graph connector status request failed")]
        public static partial void StatusRequestFailed(ILogger logger, Exception exception);
    }
}
