using System.Diagnostics;
using Aura.Application.Ports;
using Aura.Application.UseCases.Calendar;
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
        group.MapGet("/preview", GetDashboardPreviewAsync);
        group.MapGet("/system-status", GetSystemStatusAsync);
        group.MapGet("/module-progress", GetModuleProgressAsync);
        group.MapGet("/upcoming-meetings", GetUpcomingMeetingsAsync);
        group.MapGet("/today-calendar", GetTodayCalendarAsync);

        return endpoints;
    }

    private static async Task<IResult> GetSystemStatusAsync(
        ISystemStatusReader systemStatusReader,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.Dashboard");

        using var activity = ActivitySource.StartActivity("dashboard.system-status.read", ActivityKind.Server);
        activity?.SetTag("dashboard.endpoint", "/api/dashboard/system-status");

        try
        {
            var status = await systemStatusReader.GetStatusAsync(cancellationToken);
            activity?.SetTag("dashboard.system_status.api", status.Api.State.ToString());
            activity?.SetTag("dashboard.system_status.qdrant", status.Qdrant.State.ToString());
            activity?.SetTag("dashboard.system_status.mock_auth", status.MockAuth.State.ToString());

            Log.SystemStatusSucceeded(logger, status.Api.State, status.Qdrant.State, status.MockAuth.State);

            return Results.Ok(status);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.SystemStatusCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.SystemStatusFailed(logger, ex);
            return Results.Problem(title: "System status request failed", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetModuleProgressAsync(
        IModuleProgressReader moduleProgressReader,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.Dashboard");

        using var activity = ActivitySource.StartActivity("dashboard.module-progress.read", ActivityKind.Server);
        activity?.SetTag("dashboard.endpoint", "/api/dashboard/module-progress");

        try
        {
            var progress = await moduleProgressReader.GetAsync(cancellationToken);
            activity?.SetTag("dashboard.module_progress.count", progress.Entries.Count);
            activity?.SetTag("dashboard.module_progress.seeded", progress.IsSeeded);

            Log.ModuleProgressSucceeded(logger, progress.Entries.Count, progress.IsSeeded);

            return Results.Ok(progress);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.ModuleProgressCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.ModuleProgressFailed(logger, ex);
            return Results.Problem(title: "Module progress request failed", statusCode: StatusCodes.Status500InternalServerError);
        }
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

    private static async Task<IResult> GetDashboardPreviewAsync(
        IDashboardPreviewReader dashboardPreviewReader,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.Dashboard");

        using var activity = ActivitySource.StartActivity("dashboard.preview.read", ActivityKind.Server);
        activity?.SetTag("dashboard.endpoint", "/api/dashboard/preview");

        try
        {
            var preview = await dashboardPreviewReader.GetAsync(cancellationToken);
            activity?.SetTag("dashboard.preview.inbox_group_count", preview.InboxGroups.Count);
            activity?.SetTag("dashboard.preview.summary_entry_count", preview.SummaryEntries.Count);

            // ── Compute priority fields ──────────────────────────────────
            var allItems = preview.InboxGroups
                .SelectMany(g => g.Items)
                .ToList();

            var totalPendingCount = allItems.Count;
            var highPriorityCount = allItems.Count(i => ResolveEffectivePriorityScore(i) >= 75);

            var topItems = allItems
                .OrderByDescending(ResolveEffectivePriorityScore)
                .ThenByDescending(i => i.CapturedAtUtc)
                .ToList();

            var cutoffScore = topItems.Count >= 3
                ? ResolveEffectivePriorityScore(topItems[2])
                : int.MinValue;

            var highlighted = topItems
                .Where(i => ResolveEffectivePriorityScore(i) >= cutoffScore)
                .ToList();

            var enriched = preview with
            {
                TotalPendingCount = totalPendingCount,
                HighPriorityCount = highPriorityCount,
                TopItems = highlighted,
            };

            Log.DashboardPreviewSucceeded(logger, preview.InboxGroups.Count, preview.SummaryEntries.Count);

            return Results.Ok(enriched);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.DashboardPreviewCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.DashboardPreviewFailed(logger, ex);
            return Results.Problem(title: "Dashboard preview request failed", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static int ResolveEffectivePriorityScore(Aura.Application.Models.InboxItemPreviewDto item)
    {
        if (item.PriorityScore.HasValue)
        {
            return item.PriorityScore.Value;
        }

        return item.PriorityHint switch
        {
            "Critical" => 100,
            "High" => 75,
            "Medium" => 50,
            "Low" => 25,
            _ => 0
        };
    }

    private static async Task<IResult> GetUpcomingMeetingsAsync(
        GetUpcomingMeetingsUseCase useCase,
        ICurrentUserService currentUserService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.Dashboard");

        using var activity = ActivitySource.StartActivity("dashboard.upcoming-meetings.read", ActivityKind.Server);
        activity?.SetTag("dashboard.endpoint", "/api/dashboard/upcoming-meetings");

        try
        {
            var currentUser = currentUserService.GetCurrentUser();
            var userId = currentUser?.Oid;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var now = DateTimeOffset.UtcNow;
            var dtos = await useCase.ExecuteAsync(userId, now, now.AddHours(12), cancellationToken);

            activity?.SetTag("dashboard.upcoming_meetings.count", dtos.Count);

            Log.UpcomingMeetingsSucceeded(logger, dtos.Count);

            return Results.Ok(dtos);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.UpcomingMeetingsCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.UpcomingMeetingsFailed(logger, ex);
            return Results.Problem(title: "Upcoming meetings request failed", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<IResult> GetTodayCalendarAsync(
        GetUpcomingMeetingsUseCase useCase,
        ICurrentUserService currentUserService,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger("Aura.Api.Dashboard");

        using var activity = ActivitySource.StartActivity("dashboard.today-calendar.read", ActivityKind.Server);
        activity?.SetTag("dashboard.endpoint", "/api/dashboard/today-calendar");

        try
        {
            var currentUser = currentUserService.GetCurrentUser();
            var userId = currentUser?.Oid;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var now = DateTimeOffset.UtcNow;
            var dayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
            var dayEnd = dayStart.AddDays(1);
            var dtos = await useCase.ExecuteAsync(userId, dayStart, dayEnd, cancellationToken);

            activity?.SetTag("dashboard.today_calendar.count", dtos.Count);

            Log.TodayCalendarSucceeded(logger, dtos.Count);

            return Results.Ok(dtos);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Request cancelled");
            Log.TodayCalendarCancelled(logger);
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Log.TodayCalendarFailed(logger, ex);
            return Results.Problem(title: "Today calendar request failed", statusCode: StatusCodes.Status500InternalServerError);
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

        [LoggerMessage(EventId = 1004, Level = LogLevel.Information,
            Message = "System status returned Api={ApiState}, Qdrant={QdrantState}, MockAuth={MockAuthState}")]
        public static partial void SystemStatusSucceeded(
            ILogger logger,
            Aura.Application.Models.SystemIndicatorState apiState,
            Aura.Application.Models.SystemIndicatorState qdrantState,
            Aura.Application.Models.SystemIndicatorState mockAuthState);

        [LoggerMessage(EventId = 1005, Level = LogLevel.Warning,
            Message = "System status request was cancelled")]
        public static partial void SystemStatusCancelled(ILogger logger);

        [LoggerMessage(EventId = 1006, Level = LogLevel.Error,
            Message = "System status request failed")]
        public static partial void SystemStatusFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1007, Level = LogLevel.Information,
            Message = "Module progress returned {EntryCount} entries (Seeded={IsSeeded})")]
        public static partial void ModuleProgressSucceeded(ILogger logger, int entryCount, bool isSeeded);

        [LoggerMessage(EventId = 1008, Level = LogLevel.Warning,
            Message = "Module progress request was cancelled")]
        public static partial void ModuleProgressCancelled(ILogger logger);

        [LoggerMessage(EventId = 1009, Level = LogLevel.Error,
            Message = "Module progress request failed")]
        public static partial void ModuleProgressFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1010, Level = LogLevel.Information,
            Message = "Dashboard preview returned {InboxGroupCount} inbox groups and {SummaryEntryCount} summary entries")]
        public static partial void DashboardPreviewSucceeded(ILogger logger, int inboxGroupCount, int summaryEntryCount);

        [LoggerMessage(EventId = 1011, Level = LogLevel.Warning,
            Message = "Dashboard preview request was cancelled")]
        public static partial void DashboardPreviewCancelled(ILogger logger);

        [LoggerMessage(EventId = 1012, Level = LogLevel.Error,
            Message = "Dashboard preview request failed")]
        public static partial void DashboardPreviewFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1013, Level = LogLevel.Information,
            Message = "Upcoming meetings returned {MeetingCount} meetings")]
        public static partial void UpcomingMeetingsSucceeded(ILogger logger, int meetingCount);

        [LoggerMessage(EventId = 1014, Level = LogLevel.Warning,
            Message = "Upcoming meetings request was cancelled")]
        public static partial void UpcomingMeetingsCancelled(ILogger logger);

        [LoggerMessage(EventId = 1015, Level = LogLevel.Error,
            Message = "Upcoming meetings request failed")]
        public static partial void UpcomingMeetingsFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1016, Level = LogLevel.Information,
            Message = "Today calendar returned {MeetingCount} events")]
        public static partial void TodayCalendarSucceeded(ILogger logger, int meetingCount);

        [LoggerMessage(EventId = 1017, Level = LogLevel.Warning,
            Message = "Today calendar request was cancelled")]
        public static partial void TodayCalendarCancelled(ILogger logger);

        [LoggerMessage(EventId = 1018, Level = LogLevel.Error,
            Message = "Today calendar request failed")]
        public static partial void TodayCalendarFailed(ILogger logger, Exception exception);
    }
}
