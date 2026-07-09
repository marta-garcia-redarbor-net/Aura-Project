using System.Diagnostics;
using Aura.Application.Mapping;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Endpoints;

public static partial class PullRequestsEndpoints
{
    private static readonly ActivitySource ActivitySource = new("Aura.Api");

    public static IEndpointRouteBuilder MapPullRequestsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/pull-requests")
            .RequireAuthorization("RequireEntraId");

        group.MapGet("/", GetPullRequestsAsync);

        return endpoints;
    }

    private static async Task<IResult> GetPullRequestsAsync(
        IWorkItemReader workItemReader,
        ICurrentUserService currentUserService,
        ILoggerFactory loggerFactory,
        string? ownerUserId,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.PullRequests");
        var currentUser = currentUserService.GetCurrentUser();
        var currentUserOid = currentUser?.Oid;
        var currentUserDisplayName = currentUser?.DisplayName;

        using var activity = ActivitySource.StartActivity("pullrequests.read", ActivityKind.Server);
        activity?.SetTag("pullrequests.endpoint", "/api/pull-requests");

        try
        {
            var items = await workItemReader.ReadBySourceAsync(
                WorkItemSourceType.PrReview, WorkItemStatus.Pending, cancellationToken);

            var filtered = ownerUserId is not null
                ? items.Where(i => i.OwnerUserId is null || i.OwnerUserId == ownerUserId).ToList()
                : items;

            var ordered = filtered
                .OrderByDescending(i => i.PriorityScore ?? int.MinValue)
                .ThenByDescending(i => i.CapturedAtUtc)
                .ToList();

            var dtos = ordered
                .Select(item => PullRequestMapper.ToDto(item, currentUserOid, currentUserDisplayName))
                .ToArray();

            activity?.SetTag("pullrequests.count", dtos.Length);
            activity?.SetTag("pullrequests.owner_filter", ownerUserId ?? "(none)");

            Log.PullRequestsSucceeded(logger, dtos.Length);

            return Results.Ok(dtos);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.PullRequestsCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.PullRequestsFailed(logger, ex);
            return Results.Problem(title: "Pull requests request failed", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 2101, Level = LogLevel.Information,
            Message = "Pull requests returned {Count} items")]
        public static partial void PullRequestsSucceeded(ILogger logger, int count);

        [LoggerMessage(EventId = 2102, Level = LogLevel.Warning,
            Message = "Pull requests request was cancelled")]
        public static partial void PullRequestsCancelled(ILogger logger);

        [LoggerMessage(EventId = 2103, Level = LogLevel.Error,
            Message = "Pull requests request failed")]
        public static partial void PullRequestsFailed(ILogger logger, Exception exception);
    }
}
