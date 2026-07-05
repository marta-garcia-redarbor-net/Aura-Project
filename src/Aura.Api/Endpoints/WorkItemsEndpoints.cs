using System.Diagnostics;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Endpoints;

public static partial class WorkItemsEndpoints
{
    private static readonly ActivitySource ActivitySource = new("Aura.Api");

    public static IEndpointRouteBuilder MapWorkItemsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/workitems")
            .RequireAuthorization();

        group.MapGet("/", GetWorkItemsAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkItemsAsync(
        IWorkItemReader workItemReader,
        ILoggerFactory loggerFactory,
        string sourceType,
        string? status,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.WorkItems");

        using var activity = ActivitySource.StartActivity("workitems.read", ActivityKind.Server);
        activity?.SetTag("workitems.endpoint", "/api/workitems");
        activity?.SetTag("workitems.source_type", sourceType);

        try
        {
            if (!Enum.TryParse<WorkItemSourceType>(sourceType, ignoreCase: true, out var parsedSourceType))
            {
                return Results.Problem(
                    title: "Invalid source type",
                    detail: $"Source type '{sourceType}' is not valid. Valid values: {string.Join(", ", Enum.GetNames<WorkItemSourceType>())}",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            WorkItemStatus? statusFilter = null;
            if (status is not null)
            {
                if (!Enum.TryParse<WorkItemStatus>(status, ignoreCase: true, out var parsedStatus))
                {
                    return Results.Problem(
                        title: "Invalid status",
                        detail: $"Status '{status}' is not valid. Valid values: {string.Join(", ", Enum.GetNames<WorkItemStatus>())}",
                        statusCode: StatusCodes.Status400BadRequest);
                }
                statusFilter = parsedStatus;
            }

            var items = await workItemReader.ReadBySourceAsync(parsedSourceType, statusFilter, cancellationToken);

            activity?.SetTag("workitems.count", items.Count);

            var now = DateTimeOffset.UtcNow;
            var dtos = items.Select(item => ToDetailDto(item, now)).ToArray();

            Log.WorkItemsSucceeded(logger, parsedSourceType.ToString(), dtos.Length);

            return Results.Ok(dtos);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.WorkItemsCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.WorkItemsFailed(logger, ex);
            return Results.Problem(title: "Work items request failed", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static WorkItemDetailDto ToDetailDto(WorkItem item, DateTimeOffset now)
    {
        var metadataPrefix = item.SourceType switch
        {
            WorkItemSourceType.TeamsMessage => "teams",
            WorkItemSourceType.OutlookEmail => "outlook",
            WorkItemSourceType.SlackMessage => "slack",
            WorkItemSourceType.PrReview => "github",
            _ => item.Source.Trim().ToLowerInvariant()
        };

        return new WorkItemDetailDto(
            item.Id,
            item.ExternalId,
            item.Title,
            item.Source,
            item.SourceType.ToString(),
            item.Status.ToString(),
            item.Priority.ToString(),
            ToRelativeTimestamp(item.CapturedAtUtc, now),
            item.CapturedAtUtc)
        {
            Sender = ExtractMetadata(item, metadataPrefix, "sender"),
            Channel = ExtractMetadata(item, metadataPrefix, "channelId"),
            Snippet = ExtractMetadata(item, metadataPrefix, "snippet"),
            DeepLink = ExtractMetadata(item, metadataPrefix, "deepLink"),
            SuggestedAction = BuildSuggestedAction(item.Source),
            PriorityScore = item.PriorityScore
        };
    }

    private static string? ExtractMetadata(WorkItem item, string sourcePrefix, string field)
    {
        var sourceKey = $"{sourcePrefix}.{field}";
        if (item.Metadata.TryGetValue(sourceKey, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return null;
    }

    private static string BuildSuggestedAction(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return "Review and triage";

        return source.Trim().ToLowerInvariant() switch
        {
            "teams" => "Review and respond",
            "outlook" => "Review and reply",
            "github" => "Review and prioritize",
            _ => "Review and triage"
        };
    }

    private static string ToRelativeTimestamp(DateTimeOffset capturedAtUtc, DateTimeOffset nowUtc)
    {
        var elapsed = nowUtc - capturedAtUtc;
        if (elapsed < TimeSpan.Zero)
            elapsed = TimeSpan.Zero;

        if (elapsed.TotalMinutes < 1)
            return "just now";
        if (elapsed.TotalHours < 1)
            return $"{(int)elapsed.TotalMinutes}m ago";
        if (elapsed.TotalDays < 1)
            return $"{(int)elapsed.TotalHours}h ago";

        return $"{(int)elapsed.TotalDays}d ago";
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
            Message = "Work items for {SourceType} returned {Count} items")]
        public static partial void WorkItemsSucceeded(ILogger logger, string sourceType, int count);

        [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
            Message = "Work items request was cancelled")]
        public static partial void WorkItemsCancelled(ILogger logger);

        [LoggerMessage(EventId = 2003, Level = LogLevel.Error,
            Message = "Work items request failed")]
        public static partial void WorkItemsFailed(ILogger logger, Exception exception);
    }
}
