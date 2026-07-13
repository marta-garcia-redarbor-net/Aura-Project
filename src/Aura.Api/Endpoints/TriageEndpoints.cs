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
            .RequireAuthorization("RequireEntraOrDemo");

        group.MapGet("/decisions", GetDecisionsAsync);

        return endpoints;
    }

    private static async Task<IResult> GetDecisionsAsync(
        IInterruptionDecisionStore decisionStore,
        ICurrentUserService currentUserService,
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
            var currentUser = currentUserService.GetCurrentUser();
            var oid = currentUser?.Oid;

            var result = await decisionStore.QueryAsync(page, pageSize, oid, cancellationToken);
            var mapped = new PagedResult<DecisionLogItemResponse>
            {
                Items = result.Items.Select(MapToResponse).ToList(),
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize
            };

            activity?.SetTag("triage.total_count", mapped.TotalCount);
            activity?.SetTag("triage.item_count", mapped.Items.Count);

            Log.DecisionsSucceeded(logger, mapped.TotalCount, mapped.Items.Count);

            return Results.Ok(mapped);
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

    private static DecisionLogItemResponse MapToResponse(InterruptionDecisionRecord record)
        => new(
            WorkItemId: record.WorkItemId,
            Title: record.Title,
            SourceType: record.SourceType,
            Decision: record.Decision,
            PriorityScore: record.PriorityScore,
            Explanation: record.Explanation,
            Timestamp: record.Timestamp,
            FocusState: record.FocusState,
            RetrievedSemanticContext: record.RetrievedSemanticContext?.Select(c =>
                new DecisionContextItemResponse(
                    c.CanonicalSourceId,
                    c.ContentSnippet,
                    c.SourceType,
                    c.RelevanceScore)).ToList(),
            LlmRationale: record.LlmRationale,
            GuardrailOutcome: record.GuardrailOutcome);

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

    private sealed record DecisionLogItemResponse(
        Guid WorkItemId,
        string Title,
        string SourceType,
        string Decision,
        int? PriorityScore,
        string Explanation,
        DateTimeOffset Timestamp,
        string FocusState,
        IReadOnlyList<DecisionContextItemResponse>? RetrievedSemanticContext,
        string? LlmRationale,
        string? GuardrailOutcome);

    private sealed record DecisionContextItemResponse(
        string CanonicalSourceId,
        string ContentSnippet,
        string SourceType,
        double RelevanceScore);
}
