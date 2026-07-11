using Aura.Application.Demo;
using Aura.Application.Models;
using Aura.Application.Ports;
using Aura.Domain.WorkItems;
using Aura.Infrastructure.Adapters.Options;
using Microsoft.AspNetCore.SignalR;
using Aura.Api.Hubs;
using Microsoft.Extensions.Options;

namespace Aura.Api.Services;

public sealed class DemoSimulationService
{
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
            _logger.LogInformation("[0s] 📧 Outlook: Budget Review...");
            await AddAndEvaluateAsync($"demo-outlook-001-{run}",
                d => d.AddOutlookItemAsync($"demo-outlook-001-{run}", "Q3 Budget Review — Action Required", WorkItemPriority.Medium, false, ct, ownerUserId: userId),
                ct);
            await Task.Delay(3000, ct);

            // t=3s — Teams CRÍTICO with action needed + critical urgency → INTERRUPT.
            _logger.LogInformation("[3s] 🔴🔴🔴 Teams CRITICAL: Production incident!");
            await AddAndEvaluateAsync($"demo-teams-001-{run}",
                d => d.AddTeamsItemAsync($"demo-teams-001-{run}", "URGENTE: Production pipeline caído — responder YA", WorkItemPriority.Critical, true, ct, ownerUserId: userId, actionNeeded: true, timeCriticality: SignalLevel.Critical),
                ct);
            await Task.Delay(3000, ct);

            // t=6s — Nueva reunión → card de calendario se actualiza
            _logger.LogInformation("[6s] 📅 New meeting: Sprint Planning...");
            await UsingDemoAsync(d => d.AddCalendarEventAsync("demo-cal-001", "Sprint Planning — Sprint 12", now.AddHours(1), ct, userId), ct);
            await NotifyDashboardAsync("calendar", ct);
            await Task.Delay(3000, ct);

            // t=9s — 2 Pull Requests. First is high with action needed → INTERRUPT candidate, second is high without.
            _logger.LogInformation("[9s] 🔄 PRs arriving...");
            await AddAndEvaluateAsync($"demo-pr-001-{run}",
                d => d.AddPullRequestAsync($"demo-pr-001-{run}", "PR #428: feat: add caching layer", WorkItemPriority.High, ct, ownerUserId: userId, actionNeeded: true, timeCriticality: SignalLevel.High),
                ct);
            await Task.Delay(500, ct);
            await AddAndEvaluateAsync($"demo-pr-002-{run}",
                d => d.AddPullRequestAsync($"demo-pr-002-{run}", "PR #430: fix: resolve race condition", WorkItemPriority.High, ct, ownerUserId: userId),
                ct);
            await Task.Delay(3000, ct);

            // t=12s — Outlook medium, no signals → QUEUE.
            _logger.LogInformation("[12s] 📧 Outlook: Weekly Status...");
            await AddAndEvaluateAsync($"demo-outlook-002-{run}",
                d => d.AddOutlookItemAsync($"demo-outlook-002-{run}", "Weekly Status Update — EOW Report", WorkItemPriority.Medium, false, ct, ownerUserId: userId),
                ct);
            await Task.Delay(3000, ct);

            // t=15s — Teams mensaje normal → QUEUE.
            _logger.LogInformation("[15s] 💬 Teams: Standup reminder...");
            await AddAndEvaluateAsync($"demo-teams-002-{run}",
                d => d.AddTeamsItemAsync($"demo-teams-002-{run}", "Daily standup en 5 minutos — Sala virtual", WorkItemPriority.Medium, false, ct, ownerUserId: userId),
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
