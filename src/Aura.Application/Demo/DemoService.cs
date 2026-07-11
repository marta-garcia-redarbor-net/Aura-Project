using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Aura.Domain.WorkItems;

namespace Aura.Application.Demo;

/// <summary>
/// Demo use case that seeds realistic but fake data through existing port interfaces.
/// Used by demo endpoints to simulate Morning Summary, emails, Teams messages, calendar events,
/// priority alerts, and pull request arrivals.
/// </summary>
public sealed class DemoService
{
    private readonly IWorkItemStore _workItemStore;
    private readonly IMeetingAlertStore _meetingAlertStore;
    private readonly IMorningSummaryEmissionStore _morningSummaryEmissionStore;
    private readonly INotificationOutboxStore _notificationOutboxStore;
    private readonly ICalendarEventStore _calendarEventStore;
    private readonly IDashboardRefreshDispatcher _dashboardRefreshDispatcher;
    private readonly IInterruptionDecisionStore _decisionStore;
    private readonly IInterruptionPolicyEngine _interruptionEngine;

    public DemoService(
        IWorkItemStore workItemStore,
        IMeetingAlertStore meetingAlertStore,
        IMorningSummaryEmissionStore morningSummaryEmissionStore,
        INotificationOutboxStore notificationOutboxStore,
        ICalendarEventStore calendarEventStore,
        IDashboardRefreshDispatcher dashboardRefreshDispatcher,
        IInterruptionDecisionStore decisionStore,
        IInterruptionPolicyEngine interruptionEngine)
    {
        _workItemStore = workItemStore ?? throw new ArgumentNullException(nameof(workItemStore));
        _meetingAlertStore = meetingAlertStore ?? throw new ArgumentNullException(nameof(meetingAlertStore));
        _morningSummaryEmissionStore = morningSummaryEmissionStore ?? throw new ArgumentNullException(nameof(morningSummaryEmissionStore));
        _notificationOutboxStore = notificationOutboxStore ?? throw new ArgumentNullException(nameof(notificationOutboxStore));
        _calendarEventStore = calendarEventStore ?? throw new ArgumentNullException(nameof(calendarEventStore));
        _dashboardRefreshDispatcher = dashboardRefreshDispatcher ?? throw new ArgumentNullException(nameof(dashboardRefreshDispatcher));
        _decisionStore = decisionStore ?? throw new ArgumentNullException(nameof(decisionStore));
        _interruptionEngine = interruptionEngine ?? throw new ArgumentNullException(nameof(interruptionEngine));
    }

    /// <summary>
    /// Simulates Morning Summary generation by marking today's emission as done.
    /// </summary>
    public async Task<string> LoadMorningSummaryAsync(string userId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await _morningSummaryEmissionStore.MarkEmittedAsync(userId, today, ct);
        return $"Morning Summary marked as emitted for {userId} on {today:yyyy-MM-dd}";
    }

    /// <summary>
    /// Simulates arrival of 3 email work items from Outlook.
    /// </summary>
    public async Task<string> LoadEmailsAsync(CancellationToken ct, string? ownerUserId = null)
    {
        var emails = new[]
        {
            CreateWorkItem("demo-email-001", "Q3 Budget Review — Action Required", WorkItemSourceType.OutlookEmail, WorkItemPriority.High,
                new Dictionary<string, string>
                {
                    [WorkItemSignalKeys.OutlookSender] = "finance@contoso.com",
                    [WorkItemSignalKeys.OutlookSnippet] = "Q3 Budget Review — Action Required",
                    [WorkItemSignalKeys.OutlookDeepLink] = $"https://outlook.office.com/mail/demo-email-001",
                    [WorkItemSignalKeys.OutlookConversationId] = "conv-demo-001",
                    [WorkItemSignalKeys.OutlookImportanceRaw] = "high"
                }, ownerUserId: ownerUserId),
            CreateWorkItem("demo-email-002", "Architecture Decision Record: Event Sourcing", WorkItemSourceType.OutlookEmail, WorkItemPriority.Medium,
                new Dictionary<string, string>
                {
                    [WorkItemSignalKeys.OutlookSender] = "arch-team@contoso.com",
                    [WorkItemSignalKeys.OutlookSnippet] = "Architecture Decision Record: Event Sourcing",
                    [WorkItemSignalKeys.OutlookDeepLink] = $"https://outlook.office.com/mail/demo-email-002",
                    [WorkItemSignalKeys.OutlookConversationId] = "conv-demo-002",
                    [WorkItemSignalKeys.OutlookImportanceRaw] = "normal"
                }, ownerUserId: ownerUserId),
            CreateWorkItem("demo-email-003", "Weekly Status Update", WorkItemSourceType.OutlookEmail, WorkItemPriority.Low,
                new Dictionary<string, string>
                {
                    [WorkItemSignalKeys.OutlookSender] = "pm@contoso.com",
                    [WorkItemSignalKeys.OutlookSnippet] = "Weekly Status Update",
                    [WorkItemSignalKeys.OutlookDeepLink] = $"https://outlook.office.com/mail/demo-email-003",
                    [WorkItemSignalKeys.OutlookConversationId] = "conv-demo-003",
                    [WorkItemSignalKeys.OutlookImportanceRaw] = "normal"
                }, ownerUserId: ownerUserId),
        };

        foreach (var email in emails)
        {
            await _workItemStore.SaveAsync(email, ct);
            await EvaluateWorkItemAsync(email, ct);
        }

        await _dashboardRefreshDispatcher.DispatchAsync(ownerUserId, ct);

        return $"Loaded {emails.Length} demo email work items";
    }

    /// <summary>
    /// Simulates arrival of 3 Teams message work items.
    /// </summary>
    public async Task<string> LoadTeamsMessagesAsync(CancellationToken ct, string? ownerUserId = null)
    {
        var messages = new[]
        {
            CreateWorkItem("demo-teams-001", "@mention: Please review PR #428", WorkItemSourceType.TeamsMessage, WorkItemPriority.High,
                new Dictionary<string, string>
                {
                    [WorkItemSignalKeys.TeamsSender] = "alice@contoso.com",
                    [WorkItemSignalKeys.TeamsSnippet] = "@mention: Please review PR #428",
                    [WorkItemSignalKeys.TeamsTeamId] = "engineering",
                    [WorkItemSignalKeys.TeamsChannelId] = "general",
                    [WorkItemSignalKeys.TeamsDeepLink] = "https://teams.microsoft.com/l/message/demo-teams-001",
                    [WorkItemSignalKeys.TeamsPriorityRaw] = "High",
                    [WorkItemSignalKeys.TeamsPriorityResolution] = "explicit"
                }, ownerUserId: ownerUserId),
            CreateWorkItem("demo-teams-002", "Sprint planning reminder", WorkItemSourceType.TeamsMessage, WorkItemPriority.Medium,
                new Dictionary<string, string>
                {
                    [WorkItemSignalKeys.TeamsSender] = "bob@contoso.com",
                    [WorkItemSignalKeys.TeamsSnippet] = "Sprint planning reminder",
                    [WorkItemSignalKeys.TeamsTeamId] = "team-standup",
                    [WorkItemSignalKeys.TeamsChannelId] = "general",
                    [WorkItemSignalKeys.TeamsDeepLink] = "https://teams.microsoft.com/l/message/demo-teams-002",
                    [WorkItemSignalKeys.TeamsPriorityRaw] = "Medium",
                    [WorkItemSignalKeys.TeamsPriorityResolution] = "explicit"
                }, ownerUserId: ownerUserId),
            CreateWorkItem("demo-teams-003", "Quick question about API contract", WorkItemSourceType.TeamsMessage, WorkItemPriority.Low,
                new Dictionary<string, string>
                {
                    [WorkItemSignalKeys.TeamsSender] = "carol@contoso.com",
                    [WorkItemSignalKeys.TeamsSnippet] = "Quick question about API contract",
                    [WorkItemSignalKeys.TeamsTeamId] = "backend-dev",
                    [WorkItemSignalKeys.TeamsChannelId] = "general",
                    [WorkItemSignalKeys.TeamsDeepLink] = "https://teams.microsoft.com/l/message/demo-teams-003",
                    [WorkItemSignalKeys.TeamsPriorityRaw] = "Low",
                    [WorkItemSignalKeys.TeamsPriorityResolution] = "explicit"
                }, ownerUserId: ownerUserId),
        };

        foreach (var msg in messages)
        {
            await _workItemStore.SaveAsync(msg, ct);
            await EvaluateWorkItemAsync(msg, ct);
        }

        await _dashboardRefreshDispatcher.DispatchAsync(ownerUserId, ct);

        return $"Loaded {messages.Length} demo Teams message work items";
    }

    /// <summary>
    /// Simulates calendar events by querying upcoming meeting alerts.
    /// </summary>
    public async Task<string> LoadCalendarEventsAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var endOfDay = now.Date.AddDays(1);
        var upcoming = await _meetingAlertStore.GetUpcomingAlertsAsync(now, endOfDay, ct);

        return $"Loaded calendar events — {upcoming.Count} upcoming meeting alerts found";
    }

    /// <summary>
    /// Simulates 2 high-priority work items with notification outbox entries.
    /// </summary>
    public async Task<string> LoadPriorityAlertsAsync(CancellationToken ct, string? ownerUserId = null)
    {
        var criticalItem = CreateWorkItem("demo-priority-001", "PRODUCTION: Deployment pipeline failure", WorkItemSourceType.OutlookEmail, WorkItemPriority.Critical,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookSender] = "ci-bot@contoso.com",
                [WorkItemSignalKeys.OutlookSnippet] = "PRODUCTION: Deployment pipeline failure",
                [WorkItemSignalKeys.OutlookDeepLink] = "https://outlook.office.com/mail/demo-priority-001",
                [WorkItemSignalKeys.OutlookConversationId] = "conv-priority-001",
                [WorkItemSignalKeys.OutlookImportanceRaw] = "high",
                ["outlook.scoring.totalScore"] = "10"
            }, ownerUserId: ownerUserId);
        var highItem = CreateWorkItem("demo-priority-002", "Security vulnerability in dependency", WorkItemSourceType.OutlookEmail, WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookSender] = "security@contoso.com",
                [WorkItemSignalKeys.OutlookSnippet] = "Security vulnerability in dependency",
                [WorkItemSignalKeys.OutlookDeepLink] = "https://outlook.office.com/mail/demo-priority-002",
                [WorkItemSignalKeys.OutlookConversationId] = "conv-priority-002",
                [WorkItemSignalKeys.OutlookImportanceRaw] = "high",
                ["outlook.scoring.totalScore"] = "8"
            }, ownerUserId: ownerUserId);

        await _workItemStore.SaveAsync(criticalItem, ct);
        await EvaluateWorkItemAsync(criticalItem, ct);
        await _workItemStore.SaveAsync(highItem, ct);
        await EvaluateWorkItemAsync(highItem, ct);

        await _notificationOutboxStore.EnqueueAsync(
            new NotificationOutboxEntry(criticalItem.Id, ownerUserId ?? "demo-user", "OutlookEmail", criticalItem.Title, 10.0, "PriorityAlert"),
            ct);
        await _notificationOutboxStore.EnqueueAsync(
            new NotificationOutboxEntry(highItem.Id, ownerUserId ?? "demo-user", "OutlookEmail", highItem.Title, 8.0, "PriorityAlert"),
            ct);

        await _dashboardRefreshDispatcher.DispatchAsync(ownerUserId, ct);

        return "Loaded 2 priority alert work items with notifications";
    }

    /// <summary>
    /// Simulates 2 pull request review work items.
    /// </summary>
    public async Task<string> LoadPullRequestsAsync(CancellationToken ct, string? ownerUserId = null)
    {
        var pr1 = CreateWorkItem("demo-pr-001", "PR #428: feat: add caching layer", WorkItemSourceType.PrReview, WorkItemPriority.Medium,
            new Dictionary<string, string>
            {
                ["pr.pullRequestId"] = "428",
                ["pr.status"] = "active",
                ["pr.repo"] = "aura-api",
                ["pr.author"] = "alice",
                ["pr.reviewerCount"] = "2",
                ["pr.commentCount"] = "3",
                ["pr.fileCount"] = "8",
                ["pr.isDraft"] = "false",
                ["pr.sourceLink"] = "https://dev.azure.com/auraorg/Aura/_git/aura-api/pullrequest/428",
                [PrMetadataKeys.AttentionScope] = "direct"
            }, ownerUserId: ownerUserId);
        var pr2 = CreateWorkItem("demo-pr-002", "PR #430: fix: resolve race condition in worker", WorkItemSourceType.PrReview, WorkItemPriority.High,
            new Dictionary<string, string>
            {
                ["pr.pullRequestId"] = "430",
                ["pr.status"] = "active",
                ["pr.repo"] = "aura-workers",
                ["pr.author"] = "bob",
                ["pr.reviewerCount"] = "1",
                ["pr.commentCount"] = "5",
                ["pr.fileCount"] = "3",
                ["pr.isDraft"] = "false",
                ["pr.sourceLink"] = "https://dev.azure.com/auraorg/Aura/_git/aura-workers/pullrequest/430",
                [PrMetadataKeys.AttentionScope] = "direct"
            }, ownerUserId: ownerUserId);

        await _workItemStore.SaveAsync(pr1, ct);
        await EvaluateWorkItemAsync(pr1, ct);
        await _workItemStore.SaveAsync(pr2, ct);
        await EvaluateWorkItemAsync(pr2, ct);

        await _dashboardRefreshDispatcher.DispatchAsync(ownerUserId, ct);

        return "Loaded 2 demo pull request work items";
    }

    /// <summary>
    /// Runs all demo data loading methods sequentially.
    /// </summary>
    public async Task<string> LoadAllAsync(string userId, CancellationToken ct)
    {
        await LoadMorningSummaryAsync(userId, ct);
        await LoadEmailsAsync(ct, ownerUserId: userId);
        await LoadTeamsMessagesAsync(ct, ownerUserId: userId);
        await LoadCalendarEventsAsync(ct);
        await LoadPriorityAlertsAsync(ct, ownerUserId: userId);
        await LoadPullRequestsAsync(ct, ownerUserId: userId);

        return "Demo data load complete — all seed data persisted";
    }

    /// <summary>
    /// Deletes ALL work items (not just demo-prefixed) and clears all decision records.
    /// Used by the Reset button to leave the database completely empty for a fresh start.
    /// </summary>
    public async Task<string> DeleteDemoDataAsync(CancellationToken ct)
    {
        var demoSourceTypes = new[] { WorkItemSourceType.OutlookEmail, WorkItemSourceType.TeamsMessage, WorkItemSourceType.PrReview };
        var totalRemoved = 0;

        foreach (var sourceType in demoSourceTypes)
        {
            var pendingIds = await _workItemStore.GetPendingExternalIdsAsync(sourceType, ct);

            if (pendingIds.Count > 0)
            {
                await _workItemStore.MarkCompletedAsync(pendingIds, sourceType, ct);
                totalRemoved += pendingIds.Count;
            }
        }

        await _decisionStore.ClearAsync(ct);

        await _dashboardRefreshDispatcher.DispatchAsync(null, ct);
        return $"Deleted {totalRemoved} work items and all decision records";
    }

    /// <summary>
    /// Evaluates a work item through the interruption policy engine to record a decision.
    /// </summary>
    private async Task EvaluateWorkItemAsync(WorkItem workItem, CancellationToken ct)
    {
        try
        {
            await _interruptionEngine.EvaluateAsync(workItem, ct);
        }
        catch (Exception ex)
        {
            // Log but don't fail the demo load if evaluation fails
            System.Diagnostics.Debug.WriteLine($"Failed to evaluate work item {workItem.ExternalId}: {ex.Message}");
        }
    }

    private static WorkItem CreateWorkItem(
        string externalId,
        string title,
        WorkItemSourceType sourceType,
        WorkItemPriority priority,
        Dictionary<string, string> metadata,
        string? ownerUserId = null)
    {
        // Map sourceType to the canonical source string that dashboard cards filter by
        var source = sourceType switch
        {
            WorkItemSourceType.OutlookEmail => "inbox",
            WorkItemSourceType.TeamsMessage => "messages",
            WorkItemSourceType.PrReview => "pr",
            _ => "inbox"
        };

        return new WorkItem(
            externalId: externalId,
            title: title,
            source: source,
            sourceType: sourceType,
            priority: priority,
            metadata: metadata,
            ownerUserId: ownerUserId);
    }

    /// <summary>
    /// Simulates arrival of a single Outlook email with optional notification.
    /// </summary>
    public async Task<string> AddOutlookItemAsync(string id, string title, WorkItemPriority priority, bool withNotification, CancellationToken ct, string? ownerUserId = null, bool actionNeeded = false, SignalLevel? timeCriticality = null)
    {
        var metadata = new Dictionary<string, string>
        {
            [WorkItemSignalKeys.OutlookSender] = GetSender(priority),
            [WorkItemSignalKeys.OutlookSnippet] = title,
            [WorkItemSignalKeys.OutlookDeepLink] = $"https://outlook.office.com/mail/{id}",
            [WorkItemSignalKeys.OutlookConversationId] = $"conv-{id}",
            [WorkItemSignalKeys.OutlookImportanceRaw] = priority is WorkItemPriority.Critical or WorkItemPriority.High ? "high" : "normal"
        };

        if (actionNeeded)
        {
            metadata[WorkItemSignalKeys.ActionNeededSignal] = bool.TrueString;
        }

        if (timeCriticality is not null)
        {
            metadata[WorkItemSignalKeys.TimeCriticalitySignal] = timeCriticality.Value.ToString();
        }

        var item = CreateWorkItem(id, title, WorkItemSourceType.OutlookEmail, priority, metadata, ownerUserId: ownerUserId);

        await _workItemStore.SaveAsync(item, ct);

        if (withNotification)
        {
            await _notificationOutboxStore.EnqueueAsync(
                new NotificationOutboxEntry(item.Id, ownerUserId ?? "demo-user", "OutlookEmail", title, PriorityToScore(priority), "DemoSimulation"),
                ct);
        }

        await _dashboardRefreshDispatcher.DispatchAsync(ownerUserId, ct);

        return title;
    }

    /// <summary>
    /// Simulates arrival of a single Teams message with optional notification.
    /// </summary>
    public async Task<string> AddTeamsItemAsync(string id, string title, WorkItemPriority priority, bool withNotification, CancellationToken ct, string? ownerUserId = null, bool actionNeeded = false, SignalLevel? timeCriticality = null)
    {
        var metadata = new Dictionary<string, string>
        {
            [WorkItemSignalKeys.TeamsSender] = GetSender(priority),
            [WorkItemSignalKeys.TeamsSnippet] = title,
            [WorkItemSignalKeys.TeamsTeamId] = "general",
            [WorkItemSignalKeys.TeamsChannelId] = "general",
            [WorkItemSignalKeys.TeamsDeepLink] = $"https://teams.microsoft.com/l/message/{id}",
            [WorkItemSignalKeys.TeamsPriorityRaw] = priority.ToString(),
            [WorkItemSignalKeys.TeamsPriorityResolution] = "explicit"
        };

        if (actionNeeded)
        {
            metadata[WorkItemSignalKeys.ActionNeededSignal] = bool.TrueString;
        }

        if (timeCriticality is not null)
        {
            metadata[WorkItemSignalKeys.TimeCriticalitySignal] = timeCriticality.Value.ToString();
        }

        var item = CreateWorkItem(id, title, WorkItemSourceType.TeamsMessage, priority, metadata, ownerUserId: ownerUserId);

        await _workItemStore.SaveAsync(item, ct);

        if (withNotification)
        {
            await _notificationOutboxStore.EnqueueAsync(
                new NotificationOutboxEntry(item.Id, ownerUserId ?? "demo-user", "TeamsMessage", title, PriorityToScore(priority), "DemoSimulation"),
                ct);
        }

        await _dashboardRefreshDispatcher.DispatchAsync(ownerUserId, ct);

        return title;
    }

    /// <summary>
    /// Simulates arrival of a single calendar event.
    /// </summary>
    public async Task<string> AddCalendarEventAsync(string id, string title, DateTimeOffset startUtc, CancellationToken ct, string? userId = null)
    {
        var calendarEvent = new CalendarEvent(
            Id: id,
            Title: title,
            StartUtc: startUtc,
            EndUtc: startUtc.AddHours(1),
            IsOnlineMeeting: true,
            JoinUrl: "https://teams.microsoft.com/l/meetup-join/" + id,
            Organizer: "sistema@contoso.com",
            Location: "Teams Virtual",
            OriginalTimeZone: "America/Mexico_City",
            UserId: userId);

        await _calendarEventStore.SaveAsync(calendarEvent, ct);
        return title;
    }

    /// <summary>
    /// Simulates arrival of a single pull request.
    /// </summary>
    public async Task<string> AddPullRequestAsync(string id, string title, WorkItemPriority priority, CancellationToken ct, string? ownerUserId = null, bool actionNeeded = false, SignalLevel? timeCriticality = null)
    {
        var metadata = new Dictionary<string, string>
        {
            ["pr.pullRequestId"] = id.Replace("demo-pr-", ""),
            ["pr.status"] = "active",
            ["pr.repo"] = "aura-api",
            ["pr.author"] = GetSender(priority),
            ["pr.reviewerCount"] = "2",
            ["pr.commentCount"] = "2",
            ["pr.fileCount"] = "5",
            ["pr.isDraft"] = "false",
            ["pr.sourceLink"] = $"https://dev.azure.com/auraorg/Aura/_git/aura-api/pullrequest/{id.Replace("demo-pr-", "")}",
            [PrMetadataKeys.AttentionScope] = "direct"
        };

        if (actionNeeded)
        {
            metadata[WorkItemSignalKeys.ActionNeededSignal] = bool.TrueString;
        }

        if (timeCriticality is not null)
        {
            metadata[WorkItemSignalKeys.TimeCriticalitySignal] = timeCriticality.Value.ToString();
        }

        var item = CreateWorkItem(id, title, WorkItemSourceType.PrReview, priority, metadata, ownerUserId: ownerUserId);

        await _workItemStore.SaveAsync(item, ct);
        await _dashboardRefreshDispatcher.DispatchAsync(ownerUserId, ct);
        return title;
    }

    private static string GetSender(WorkItemPriority priority) => priority switch
    {
        WorkItemPriority.Critical => "ceo@contoso.com",
        WorkItemPriority.High => "director@contoso.com",
        WorkItemPriority.Medium => "manager@contoso.com",
        _ => "sistema@contoso.com"
    };

    private static double PriorityToScore(WorkItemPriority priority) => priority switch
    {
        WorkItemPriority.Critical => 10.0,
        WorkItemPriority.High => 8.0,
        WorkItemPriority.Medium => 5.0,
        _ => 2.0
    };
}
