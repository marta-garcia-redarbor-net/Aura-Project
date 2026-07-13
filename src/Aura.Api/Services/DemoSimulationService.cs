using Aura.Application.Demo;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Options;
using Microsoft.AspNetCore.SignalR;
using Aura.Api.Hubs;
using Microsoft.Extensions.Options;

namespace Aura.Api.Services;

public sealed class DemoSimulationService
{
    private static readonly Random _rng = Random.Shared;

    private static readonly string[] OutlookTitles = ["Q3 Budget Review — Action Required", "Architecture Decision Record: Event Sourcing", "Infrastructure cost report", "Client meeting follow-up", "Performance review feedback"];
    private static readonly string[] TeamsTitles = ["URGENTE: Production pipeline caído — responder YA", "Please review PR #428 — blocking release", "Quick question about API contract", "Sprint planning reminder", "Code review request: auth module"];
    private static readonly string[] PrTitles = ["PR #428: feat: add caching layer", "PR #430: fix: resolve race condition", "PR #435: feat: add search endpoint", "PR #440: refactor: extract auth middleware"];
    private static readonly string[] MeetingTitles = ["Sprint Planning — Sprint 12", "UI Review — Design System Alignment", "Architecture Sync — Event Sourcing", "1:1 with Manager — Performance Review", "Demo Rehearsal — Client Presentation", "Backlog Refinement — Sprint 13", "Retrospective — Sprint 12"];

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DemoSimulationService> _logger;
    private readonly IOptions<DemoModeOptions> _options;
    private readonly IHubContext<AlertHub> _hubContext;
    private CancellationTokenSource? _cts;

    public DemoSimulationService(
        IServiceScopeFactory scopeFactory,
        IOptions<DemoModeOptions> options,
        ILogger<DemoSimulationService> logger,
        IHubContext<AlertHub> hubContext)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
        _hubContext = hubContext;
    }

    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;

    public void Start(string? userId)
    {
        if (IsRunning) return;
        if (!_options.Value.Enabled) return;
        _cts = new CancellationTokenSource();
        _ = RunAsync(userId, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private async Task RunAsync(string? userId, CancellationToken ct)
    {
        _logger.LogInformation("🌟 Demo simulation started for user {UserId}", userId ?? "(all users)");
        var now = DateTimeOffset.UtcNow;
        // Unique suffix per run so items accumulate instead of overwriting via ExternalId upsert
        var run = now.ToString("yyyyMMddHHmmss");

        try
        {
            // t=0s — Outlook email medium priority, no action needed → QUEUE.
            var outlookTitle1 = OutlookTitles[_rng.Next(OutlookTitles.Length)];
            _logger.LogInformation("[0s] 📧 Outlook: {Title}...", outlookTitle1);
            await AddAndEvaluateAsync($"demo-outlook-001-{run}",
                d => d.AddOutlookItemAsync($"demo-outlook-001-{run}", outlookTitle1, WorkItemPriority.Medium, false, ct, ownerUserId: userId),
                ct);
            await Task.Delay(3000, ct);

            // t=3s — Teams CRÍTICO with action needed + critical urgency → INTERRUPT.
            var teamsTitle1 = TeamsTitles[_rng.Next(TeamsTitles.Length)];
            _logger.LogInformation("[3s] 🔴🔴🔴 Teams CRITICAL: {Title}", teamsTitle1);
            await AddAndEvaluateAsync($"demo-teams-001-{run}",
                d => d.AddTeamsItemAsync($"demo-teams-001-{run}", teamsTitle1, WorkItemPriority.Critical, true, ct, ownerUserId: userId, actionNeeded: true, timeCriticality: SignalLevel.Critical),
                ct);
            await Task.Delay(3000, ct);

            // t=6s — New meeting 45-75 min from now (staggered to avoid overlap across runs)
            var meetingStart = now.AddMinutes(45 + _rng.Next(31));
            var meetingTitle = MeetingTitles[_rng.Next(MeetingTitles.Length)];
            _logger.LogInformation("[6s] 📅 New meeting: {Title}...", meetingTitle);
            await UsingDemoAsync(d => d.AddCalendarEventAsync($"demo-cal-001-{run}", meetingTitle, meetingStart, ct, userId), ct);

            // Create and dispatch meeting alert so the UI shows the toast
            await DispatchMeetingAlertAsync(userId, run, meetingTitle, meetingStart, ct);

            await NotifyDashboardAsync("calendar", ct);
            await Task.Delay(3000, ct);

            // t=9s — 2 Pull Requests. First is high with action needed → INTERRUPT candidate, second is high without.
            _logger.LogInformation("[9s] 🔄 PRs arriving...");
            var prTitle1 = PrTitles[_rng.Next(PrTitles.Length)];
            await AddAndEvaluateAsync($"demo-pr-001-{run}",
                d => d.AddPullRequestAsync($"demo-pr-001-{run}", prTitle1, WorkItemPriority.High, ct, ownerUserId: userId, actionNeeded: true, timeCriticality: SignalLevel.High),
                ct);
            await Task.Delay(500, ct);
            var prTitle2 = PrTitles[_rng.Next(PrTitles.Length)];
            await AddAndEvaluateAsync($"demo-pr-002-{run}",
                d => d.AddPullRequestAsync($"demo-pr-002-{run}", prTitle2, WorkItemPriority.High, ct, ownerUserId: userId),
                ct);
            await Task.Delay(3000, ct);

            // t=12s — Outlook medium, no signals → QUEUE.
            var outlookTitle2 = OutlookTitles[_rng.Next(OutlookTitles.Length)];
            _logger.LogInformation("[12s] 📧 Outlook: {Title}...", outlookTitle2);
            await AddAndEvaluateAsync($"demo-outlook-002-{run}",
                d => d.AddOutlookItemAsync($"demo-outlook-002-{run}", outlookTitle2, WorkItemPriority.Medium, false, ct, ownerUserId: userId),
                ct);
            await Task.Delay(3000, ct);

            // t=15s — Teams mensaje normal → QUEUE.
            var teamsTitle2 = TeamsTitles[_rng.Next(TeamsTitles.Length)];
            _logger.LogInformation("[15s] 💬 Teams: {Title}...", teamsTitle2);
            await AddAndEvaluateAsync($"demo-teams-002-{run}",
                d => d.AddTeamsItemAsync($"demo-teams-002-{run}", teamsTitle2, WorkItemPriority.Medium, false, ct, ownerUserId: userId),
                ct);

            _logger.LogInformation("✅ Demo simulation completed — 8 items injected and evaluated");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Demo simulation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo simulation failed");
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task NotifyDashboardAsync(string dataType, CancellationToken ct)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("DashboardRefresh", new
            {
                Timestamp = DateTimeOffset.UtcNow,
                DataType = dataType
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR notification");
        }
    }

    private async Task DispatchMeetingAlertAsync(string? userId, string run, string title, DateTimeOffset meetingStart, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var alertStore = scope.ServiceProvider.GetRequiredService<IMeetingAlertStore>();
        var alertDispatcher = scope.ServiceProvider.GetRequiredService<IMeetingAlertDispatcher>();

        var alert = new MeetingAlert(
            EventId: $"demo-cal-001-{run}",
            Title: title,
            Trigger: MeetingAlertTrigger.SixtyMinutes,
            StartsAtUtc: meetingStart,
            JoinUrl: $"https://teams.microsoft.com/l/meetup-join/demo-cal-001-{run}",
            UserId: userId ?? "demo-user",
            HasBeenSent: true);
        await alertStore.MarkSentAsync(alert, ct);
        await alertDispatcher.DispatchAsync(alert, ct);
    }

    private async Task UsingDemoAsync(Func<DemoService, Task> action, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var demo = scope.ServiceProvider.GetRequiredService<DemoService>();
        await action(demo);
    }

    /// <summary>
    /// Adds a work item via DemoService, then evaluates it through the policy engine
    /// so a real decision is recorded with full trace (rules + LLM advisor + Qdrant context).
    /// </summary>
    private async Task AddAndEvaluateAsync(string externalId, Func<DemoService, Task> addAction, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var demo = sp.GetRequiredService<DemoService>();
        var workItemStore = sp.GetRequiredService<IWorkItemStore>();
        var engine = sp.GetRequiredService<IInterruptionPolicyEngine>();

        // 1. Inject the work item
        await addAction(demo);

        // 2. Find the persisted work item by externalId
        var workItem = await workItemStore.FindByExternalIdAsync(externalId, ct);
        if (workItem is null)
        {
            _logger.LogWarning("Work item with ExternalId {ExternalId} not found after injection — skipping evaluation", externalId);
            return;
        }

        // 3. Evaluate through the policy engine (records decision internally)
        try
        {
            var verdict = await engine.EvaluateAsync(workItem, ct);
            _logger.LogInformation(
                "Decision recorded for {ExternalId}: {Decision} (trigger={TriggerRule}, guardrail from engine)",
                externalId, verdict.Decision, verdict.TriggerRule ?? "none");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Policy engine evaluation failed for {ExternalId} — item was injected but no decision recorded", externalId);
        }

        // 4. Notify dashboard to refresh
        await NotifyDashboardAsync("decisions", ct);
    }
}
