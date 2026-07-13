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
    private static readonly Random _rng = Random.Shared;

    private static readonly string[] EmailSenders = ["Finance Team", "Architecture Team", "Project Manager", "HR Department", "DevOps", "Security Team"];
    private static readonly string[] TeamsSenders = ["Alice Chen", "Bob Martinez", "Carol Silva", "Dave Kim", "Eve Johnson"];
    private static readonly string[] EmailSubjects = ["Q3 Budget Review", "Architecture Decision Record: Event Sourcing", "Weekly Status Update", "Updated onboarding docs", "Sprint retrospective notes", "Performance review feedback", "New feature proposal: dark mode", "Infrastructure cost report", "Client meeting follow-up", "Team building event"];
    private static readonly string[] TeamsSubjects = ["Please review PR #428", "Sprint planning reminder", "Quick question about API contract", "Production incident postmortem", "New library version available", "Code review request: auth module", "API schema change discussion", "Environment cleanup this weekend"];
    private static readonly string[] PrTitles = ["feat: add caching layer", "fix: resolve race condition in worker", "feat: add search endpoint", "refactor: extract auth middleware", "fix: correct date parsing in calendar sync", "chore: update dependencies", "feat: add rate limiting", "docs: update API reference"];
    private static readonly string[] PrRepos = ["aura-api", "aura-workers", "aura-ui", "aura-infra"];
    private static readonly string[] PrAuthors = ["Alice Chen", "Bob Martinez", "Carol Silva", "Dave Kim"];
    private static readonly string[] BranchNames = ["feat/add-caching-layer", "fix/resolve-race-condition-worker", "feat/add-search-endpoint", "refactor/extract-auth-middleware", "fix/calendar-date-parsing", "chore/update-deps", "feat/rate-limiting", "docs/api-reference"];
    private static readonly string[] MeetingTitles = ["Sprint Planning — Sprint 12", "Architecture Review: Event Sourcing", "1:1 with Manager", "Demo Prep Sync", "Incident Postmortem", "Backlog Grooming", "Design Review: Auth Flow"];

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

    private static string RandomSuffix() => DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");

    private static T PickRandom<T>(T[] items) => items[_rng.Next(items.Length)];

    private static WorkItemPriority RandomPriority() => (WorkItemPriority)_rng.Next(0, 3);

    /// <summary>
    /// Simulates arrival of 3 email work items from Outlook.
    /// </summary>
    public async Task<string> LoadEmailsAsync(CancellationToken ct, string? ownerUserId = null)
    {
        var runId = RandomSuffix();
        var priorities = new[] { WorkItemPriority.High, WorkItemPriority.Medium, WorkItemPriority.Low };
        var emails = new WorkItem[3];

        for (var i = 0; i < 3; i++)
        {
            var idx = i + 1;
            var sender = PickRandom(EmailSenders);
            var subject = PickRandom(EmailSubjects);
            var priority = priorities[i % priorities.Length];
            var externalId = $"demo-email-{idx:D3}-{runId}";

            emails[i] = CreateWorkItem(externalId, subject, WorkItemSourceType.OutlookEmail, priority,
                new Dictionary<string, string>
                {
                    [WorkItemSignalKeys.OutlookSender] = sender,
                    [WorkItemSignalKeys.OutlookSnippet] = subject,
                    [WorkItemSignalKeys.OutlookDeepLink] = $"https://outlook.office.com/mail/{externalId}",
                    [WorkItemSignalKeys.OutlookConversationId] = $"conv-{externalId}",
                    [WorkItemSignalKeys.OutlookImportanceRaw] = priority is WorkItemPriority.Critical or WorkItemPriority.High ? "high" : "normal"
                }, ownerUserId: ownerUserId);
        }

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
        var runId = RandomSuffix();
        var priorities = new[] { WorkItemPriority.High, WorkItemPriority.Medium, WorkItemPriority.Low };
        var messages = new WorkItem[3];

        for (var i = 0; i < 3; i++)
        {
            var idx = i + 1;
            var sender = PickRandom(TeamsSenders);
            var subject = PickRandom(TeamsSubjects);
            var priority = priorities[i % priorities.Length];
            var externalId = $"demo-teams-{idx:D3}-{runId}";

            messages[i] = CreateWorkItem(externalId, $"@mention: {subject}", WorkItemSourceType.TeamsMessage, priority,
                new Dictionary<string, string>
                {
                    [WorkItemSignalKeys.TeamsSender] = sender,
                    [WorkItemSignalKeys.TeamsSnippet] = subject,
                    [WorkItemSignalKeys.TeamsTeamId] = "engineering",
                    [WorkItemSignalKeys.TeamsChannelId] = "general",
                    [WorkItemSignalKeys.TeamsDeepLink] = $"https://teams.microsoft.com/l/message/{externalId}",
                    [WorkItemSignalKeys.TeamsPriorityRaw] = priority.ToString(),
                    [WorkItemSignalKeys.TeamsPriorityResolution] = "explicit"
                }, ownerUserId: ownerUserId);
        }

        foreach (var msg in messages)
        {
            await _workItemStore.SaveAsync(msg, ct);
            await EvaluateWorkItemAsync(msg, ct);
        }

        await _dashboardRefreshDispatcher.DispatchAsync(ownerUserId, ct);

        return $"Loaded {messages.Length} demo Teams message work items";
    }

    /// <summary>
    /// Creates a demo calendar event starting ~50 minutes from now.
    /// </summary>
    public async Task<string> LoadCalendarEventsAsync(CancellationToken ct)
    {
        var runId = RandomSuffix();
        var meetingStart = DateTimeOffset.UtcNow.AddMinutes(50);
        var title = PickRandom(MeetingTitles);
        var eventId = $"demo-cal-{runId}";

        await AddCalendarEventAsync(eventId, title, meetingStart, ct);

        return $"Created demo calendar event '{title}' starting at {meetingStart:HH:mm} UTC";
    }

    /// <summary>
    /// Creates and dispatches a demo meeting alert for the given event.
    /// </summary>
    public async Task<string> CreateDemoMeetingAlertAsync(string userId, string eventId, string title, DateTimeOffset startUtc, string joinUrl, CancellationToken ct)
    {
        var alert = new MeetingAlert(
            EventId: eventId,
            Title: title,
            Trigger: MeetingAlertTrigger.SixtyMinutes,
            StartsAtUtc: startUtc,
            JoinUrl: joinUrl,
            UserId: userId,
            HasBeenSent: true);

        await _meetingAlertStore.MarkSentAsync(alert, ct);
        return $"Created demo meeting alert for '{title}' (trigger: SixtyMinutes)";
    }

    /// <summary>
    /// Simulates 2 high-priority work items with notification outbox entries.
    /// </summary>
    public async Task<string> LoadPriorityAlertsAsync(CancellationToken ct, string? ownerUserId = null)
    {
        var runId = RandomSuffix();
        var criticalTitle = PickRandom(EmailSubjects);
        var highTitle = PickRandom(EmailSubjects);
        var criticalSender = PickRandom(EmailSenders);
        var highSender = PickRandom(EmailSenders);

        var criticalItem = CreateWorkItem($"demo-priority-001-{runId}", $"PRODUCTION: {criticalTitle}", WorkItemSourceType.OutlookEmail, WorkItemPriority.Critical,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookSender] = criticalSender,
                [WorkItemSignalKeys.OutlookSnippet] = $"PRODUCTION: {criticalTitle}",
                [WorkItemSignalKeys.OutlookDeepLink] = $"https://outlook.office.com/mail/demo-priority-001-{runId}",
                [WorkItemSignalKeys.OutlookConversationId] = $"conv-priority-001-{runId}",
                [WorkItemSignalKeys.OutlookImportanceRaw] = "high",
                ["outlook.scoring.totalScore"] = "10"
            }, ownerUserId: ownerUserId);
        var highItem = CreateWorkItem($"demo-priority-002-{runId}", highTitle, WorkItemSourceType.OutlookEmail, WorkItemPriority.High,
            new Dictionary<string, string>
            {
                [WorkItemSignalKeys.OutlookSender] = highSender,
                [WorkItemSignalKeys.OutlookSnippet] = highTitle,
                [WorkItemSignalKeys.OutlookDeepLink] = $"https://outlook.office.com/mail/demo-priority-002-{runId}",
                [WorkItemSignalKeys.OutlookConversationId] = $"conv-priority-002-{runId}",
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
        var runId = RandomSuffix();
        var pr1Num = _rng.Next(400, 999);
        var pr2Num = _rng.Next(400, 999);
        var title1 = PickRandom(PrTitles);
        var title2 = PickRandom(PrTitles);
        var repo1 = PickRandom(PrRepos);
        var repo2 = PickRandom(PrRepos);
        var author1 = PickRandom(PrAuthors);
        var author2 = PickRandom(PrAuthors);
        var branch1 = PickRandom(BranchNames);
        var branch2 = PickRandom(BranchNames);

        var pr1 = CreateWorkItem($"demo-pr-001-{runId}", $"PR #{pr1Num}: {title1}", WorkItemSourceType.PrReview, WorkItemPriority.Medium,
            new Dictionary<string, string>
            {
                ["pr.pullRequestId"] = pr1Num.ToString(),
                ["pr.status"] = "active",
                ["pr.repo"] = repo1,
                ["pr.author"] = author1,
                ["pr.branch"] = branch1,
                ["pr.reviewerCount"] = _rng.Next(1, 4).ToString(),
                ["pr.commentCount"] = _rng.Next(0, 10).ToString(),
                ["pr.fileCount"] = _rng.Next(2, 15).ToString(),
                ["pr.isDraft"] = "false",
                ["pr.sourceLink"] = $"https://dev.azure.com/auraorg/Aura/_git/{repo1}/pullrequest/{pr1Num}",
                [PrMetadataKeys.AttentionScope] = "direct"
            }, ownerUserId: ownerUserId);
        var pr2 = CreateWorkItem($"demo-pr-002-{runId}", $"PR #{pr2Num}: {title2}", WorkItemSourceType.PrReview, WorkItemPriority.High,
            new Dictionary<string, string>
            {
                ["pr.pullRequestId"] = pr2Num.ToString(),
                ["pr.status"] = "active",
                ["pr.repo"] = repo2,
                ["pr.author"] = author2,
                ["pr.branch"] = branch2,
                ["pr.reviewerCount"] = _rng.Next(1, 4).ToString(),
                ["pr.commentCount"] = _rng.Next(0, 10).ToString(),
                ["pr.fileCount"] = _rng.Next(2, 15).ToString(),
                ["pr.isDraft"] = "false",
                ["pr.sourceLink"] = $"https://dev.azure.com/auraorg/Aura/_git/{repo2}/pullrequest/{pr2Num}",
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
    /// Runs all demo data loading methods sequentially, resetting existing data first.
    /// </summary>
    public async Task<string> LoadAllAsync(string userId, CancellationToken ct)
    {
        await LoadMorningSummaryAsync(userId, ct);
        var calResult = await LoadCalendarEventsAsync(ct);
        await LoadEmailsAsync(ct, ownerUserId: userId);
        await LoadTeamsMessagesAsync(ct, ownerUserId: userId);
        await LoadPriorityAlertsAsync(ct, ownerUserId: userId);
        await LoadPullRequestsAsync(ct, ownerUserId: userId);

        return $"Demo data load complete — all seed data persisted. {calResult}";
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

        // Map raw signals to canonical keys so the scoring service can evaluate them
        AddCanonicalSignal(metadata, WorkItemSignalKeys.OutlookSender, WorkItemSignalKeys.CanonicalSender);
        AddCanonicalSignal(metadata, WorkItemSignalKeys.TeamsSender, WorkItemSignalKeys.CanonicalSender);
        AddCanonicalSignal(metadata, WorkItemSignalKeys.OutlookSnippet, WorkItemSignalKeys.CanonicalSnippet);
        AddCanonicalSignal(metadata, WorkItemSignalKeys.TeamsSnippet, WorkItemSignalKeys.CanonicalSnippet);

        // Set action-needed and time-criticality signals for high/critical items so
        // the scoring engine can produce varied scores (40, 80, 100) instead of 0 or 100.
        if (priority is WorkItemPriority.Critical or WorkItemPriority.High)
        {
            metadata.TryAdd(WorkItemSignalKeys.ActionNeededSignal, bool.TrueString);
            if (priority == WorkItemPriority.Critical)
            {
                metadata.TryAdd(WorkItemSignalKeys.TimeCriticalitySignal, SignalLevel.Critical.ToString());
            }
            else
            {
                metadata.TryAdd(WorkItemSignalKeys.TimeCriticalitySignal, SignalLevel.High.ToString());
            }
        }

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
            Organizer: "Aura Calendar",
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
        var branch = PickRandom(BranchNames);
        var repo = PickRandom(PrRepos);
        var author = PickRandom(PrAuthors);
        var prNum = _rng.Next(400, 999);

        var metadata = new Dictionary<string, string>
        {
            ["pr.pullRequestId"] = prNum.ToString(),
            ["pr.status"] = "active",
            ["pr.repo"] = repo,
            ["pr.author"] = author,
            ["pr.branch"] = branch,
            ["pr.reviewerCount"] = _rng.Next(1, 4).ToString(),
            ["pr.commentCount"] = _rng.Next(0, 10).ToString(),
            ["pr.fileCount"] = _rng.Next(2, 15).ToString(),
            ["pr.isDraft"] = "false",
            ["pr.sourceLink"] = $"https://dev.azure.com/auraorg/Aura/_git/{repo}/pullrequest/{prNum}",
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
        WorkItemPriority.Critical => "CTO Office",
        WorkItemPriority.High => "Director Engineering",
        WorkItemPriority.Medium => "Team Lead",
        _ => "System Bot"
    };

    private static double PriorityToScore(WorkItemPriority priority) => priority switch
    {
        WorkItemPriority.Critical => 10.0,
        WorkItemPriority.High => 8.0,
        WorkItemPriority.Medium => 5.0,
        _ => 2.0
    };

    /// <summary>
    /// Maps a raw signal key (e.g. outlook.sender) to a canonical key (e.g. triage.sender)
    /// so the priority scoring service can evaluate demo items with realistic scores.
    /// Only copies the value if the source key exists and the target key is not already set.
    /// </summary>
    private static void AddCanonicalSignal(Dictionary<string, string> metadata, string sourceKey, string targetKey)
    {
        if (metadata.TryGetValue(sourceKey, out var value) && !string.IsNullOrWhiteSpace(value) && !metadata.ContainsKey(targetKey))
        {
            metadata[targetKey] = value;
        }
    }
}
