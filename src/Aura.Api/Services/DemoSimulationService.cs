using Aura.Application.Demo;
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

        try
        {
            // t=0s — Outlook email medium priority.
            // Dashboard refresh is now emitted by the same backend dispatcher used by real work item writes.
            _logger.LogInformation("[0s] 📧 Outlook: Budget Review...");
            await UsingDemoAsync(d => d.AddOutlookItemAsync("demo-outlook-001", "Q3 Budget Review — Action Required", WorkItemPriority.Medium, false, ct, ownerUserId: userId), ct);
            await Task.Delay(3000, ct);

            // t=3s — Teams CRÍTICO.
            _logger.LogInformation("[3s] 🔴🔴🔴 Teams CRITICAL: Production incident!");
            await UsingDemoAsync(d => d.AddTeamsItemAsync("demo-teams-001", "URGENTE: Production pipeline caído — responder YA", WorkItemPriority.Critical, true, ct, ownerUserId: userId), ct);
            await Task.Delay(3000, ct);

            // t=6s — Nueva reunión → card de calendario se actualiza
            _logger.LogInformation("[6s] 📅 New meeting: Sprint Planning...");
            await UsingDemoAsync(d => d.AddCalendarEventAsync("demo-cal-001", "Sprint Planning — Sprint 12", now.AddHours(1), ct, userId), ct);
            await NotifyDashboardAsync("calendar", ct);
            await Task.Delay(3000, ct);

            // t=9s — 2 Pull Requests.
            _logger.LogInformation("[9s] 🔄 PRs arriving...");
            await UsingDemoAsync(d => d.AddPullRequestAsync("demo-pr-001", "PR #428: feat: add caching layer", WorkItemPriority.High, ct, ownerUserId: userId), ct);
            await Task.Delay(500, ct);
            await UsingDemoAsync(d => d.AddPullRequestAsync("demo-pr-002", "PR #430: fix: resolve race condition", WorkItemPriority.High, ct, ownerUserId: userId), ct);
            await Task.Delay(3000, ct);

            // t=12s — Otro Outlook medio.
            _logger.LogInformation("[12s] 📧 Outlook: Weekly Status...");
            await UsingDemoAsync(d => d.AddOutlookItemAsync("demo-outlook-002", "Weekly Status Update — EOW Report", WorkItemPriority.Medium, false, ct, ownerUserId: userId), ct);
            await Task.Delay(3000, ct);

            // t=15s — Teams mensaje normal.
            _logger.LogInformation("[15s] 💬 Teams: Standup reminder...");
            await UsingDemoAsync(d => d.AddTeamsItemAsync("demo-teams-002", "Daily standup en 5 minutos — Sala virtual", WorkItemPriority.Medium, false, ct, ownerUserId: userId), ct);

            _logger.LogInformation("✅ Demo simulation completed — 8 items injected");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Demo simulation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo simulation failed");
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
}
