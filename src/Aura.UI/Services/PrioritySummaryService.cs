using System.Diagnostics;
using Aura.UI.Models;
using Microsoft.Extensions.Logging;

namespace Aura.UI.Services;

public sealed class PrioritySummaryService : IPrioritySummaryService
{
    private static readonly ActivitySource ActivitySource = new("Aura.UI.PrioritySummary");
    private readonly IDashboardPreviewApiClient _previewApiClient;
    private readonly ICalendarApiClient _calendarApiClient;
    private readonly IAzureDevOpsPrClient _prClient;
    private readonly ILogger<PrioritySummaryService> _logger;

    public PrioritySummaryService(
        IDashboardPreviewApiClient previewApiClient,
        ICalendarApiClient calendarApiClient,
        IAzureDevOpsPrClient prClient,
        ILogger<PrioritySummaryService> logger)
    {
        _previewApiClient = previewApiClient;
        _calendarApiClient = calendarApiClient;
        _prClient = prClient;
        _logger = logger;
    }

    public async Task<List<PrioritySummaryCard>> GetCardsAsync(CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("priority-summary.build-cards", ActivityKind.Internal);
        
        try
        {
            var previewTask = _previewApiClient.GetPreviewAsync(cancellationToken);
            var calendarTask = _calendarApiClient.GetUpcomingMeetingsAsync(cancellationToken);
            var prTask = _prClient.GetPendingPullRequestsAsync(cancellationToken);

            await Task.WhenAll(previewTask, calendarTask, prTask);

            var preview = previewTask.Result;
            var calendar = calendarTask.Result;
            var prs = prTask.Result;

            var cards = BuildCards(preview, calendar, prs);
            activity?.SetTag("priority-summary.teams_count", cards[0].PreviewItems?.Count ?? 0);
            activity?.SetTag("priority-summary.outlook_count", cards[1].PreviewItems?.Count ?? 0);
            activity?.SetTag("priority-summary.calendar_count", cards[2].CalendarItems?.Count ?? 0);
            activity?.SetTag("priority-summary.pr_count", cards[3].PrItems?.Count ?? 0);
            _logger.LogDebug("Built priority summary cards: Teams={TeamsCount}, Outlook={OutlookCount}, Calendar={CalendarCount}, PRs={PrCount}",
                cards[0].PreviewItems?.Count ?? 0,
                cards[1].PreviewItems?.Count ?? 0,
                cards[2].CalendarItems?.Count ?? 0,
                cards[3].PrItems?.Count ?? 0);
            return cards;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to build priority summary cards");
            throw;
        }
    }

    private static List<PrioritySummaryCard> BuildCards(
        DashboardPreviewResponse preview,
        IReadOnlyList<UpcomingMeetingResponse> calendar,
        List<PullRequestResponse> prs)
    {
        var allItems = preview.InboxGroups
            .SelectMany(g => g.Items)
            .ToList();

        var teamsItems = allItems
            .Where(i => string.Equals(i.Source, "messages", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(i => i.Score)
            .ToList();

        var outlookItems = allItems
            .Where(i => string.Equals(i.Source, "inbox", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(i => i.Score)
            .ToList();

        var orderedCalendar = calendar
            .OrderBy(c => c.StartUtc)
            .ToList();

        var prPreviewItems = prs
            .OrderByDescending(p => p.Priority switch
            {
                "critical" => 4,
                "high" => 3,
                "medium" => 2,
                _ => 1
            })
            .ThenByDescending(p => p.UpdatedAt)
            .Select(p => new PrPreviewItemResponse(
                Title: p.Title,
                PrDisplayName: $"#{p.Id} {p.Title}",
                BranchName: p.BranchName,
                BuildStatus: p.BuildStatus,
                ReviewApprovals: p.ReviewApprovals,
                ReviewRequired: p.ReviewRequired,
                ReviewChangesRequested: p.ReviewChangesRequested,
                Author: p.Author,
                UpdatedAt: p.UpdatedAt,
                RelativeTimestamp: GetRelativeTime(p.UpdatedAt),
                SourceLink: p.SourceLink,
                IsDraft: p.IsDraft,
                Priority: p.Priority))
            .ToList();

        return
        [
            new PrioritySummaryCard(
                DisplayName: "Teams Mentions",
                Icon: "groups",
                CssClass: "teams",
                CountLabel: "NEW",
                ItemsLabel: "items",
                SourceLabel: "Open Teams",
                ViewAllUrl: "https://teams.microsoft.com",
                DetailPageUrl: "/teams",
                PreviewItems: teamsItems,
                CalendarItems: null),
            new PrioritySummaryCard(
                DisplayName: "Outlook",
                Icon: "mail",
                CssClass: "outlook",
                CountLabel: "UNREAD",
                ItemsLabel: "items",
                SourceLabel: "Open Outlook",
                ViewAllUrl: "https://outlook.office.com",
                DetailPageUrl: "/outlook",
                PreviewItems: outlookItems,
                CalendarItems: null),
            new PrioritySummaryCard(
                DisplayName: "Schedule Today",
                Icon: "calendar_today",
                CssClass: "schedule",
                CountLabel: "EVENTS",
                ItemsLabel: "meetings",
                SourceLabel: "Open Calendar",
                ViewAllUrl: "https://outlook.office.com/calendar/view/day",
                DetailPageUrl: "/calendar/day",
                PreviewItems: null,
                CalendarItems: orderedCalendar),
            new PrioritySummaryCard(
                DisplayName: "Pull Requests",
                Icon: "account_tree",
                CssClass: "pr-review",
                CountLabel: "PENDING",
                ItemsLabel: "PRs",
                SourceLabel: "View All Repositories",
                ViewAllUrl: "https://redarbor.visualstudio.com/",
                DetailPageUrl: "/pull-requests",
                PreviewItems: null,
                CalendarItems: null)
            {
                IsPrCard = true,
                PrItems = prPreviewItems
            }
        ];
    }

    public string FormatTimeRange(UpcomingMeetingResponse ev)
    {
        var start = ev.StartUtc.ToLocalTime();
        var end = ev.EndUtc.ToLocalTime();

        if (start.Date == end.Date)
        {
            return $"{start:h:mm} - {end:h:mm}";
        }

        return $"{start:M/d h:mm} - {end:M/d h:mm}";
    }

    public string GetEventStatus(UpcomingMeetingResponse ev)
    {
        var now = DateTimeOffset.UtcNow;
        if (ev.EndUtc < now) return "past";
        if (ev.StartUtc <= now) return "current";
        return "upcoming";
    }

    private static string GetRelativeTime(DateTimeOffset dateTime)
    {
        var now = DateTimeOffset.UtcNow;
        var diff = now - dateTime;

        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return dateTime.ToLocalTime().ToString("MMM d");
    }
}
