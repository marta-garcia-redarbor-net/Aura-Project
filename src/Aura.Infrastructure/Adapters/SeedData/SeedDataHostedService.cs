using Aura.Application.Ports;
using Aura.Domain.Calendar;
using Aura.Domain.WorkItems;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.Infrastructure.Adapters.SeedData;

internal sealed partial class SeedDataHostedService : IHostedService
{
    private readonly IWorkItemStore _workItemStore;
    private readonly ICalendarEventStore _calendarEventStore;
    private readonly ILogger<SeedDataHostedService> _logger;
    private readonly SeedDataOptions _options;

    public SeedDataHostedService(
        IWorkItemStore workItemStore,
        ICalendarEventStore calendarEventStore,
        IOptions<SeedDataOptions> options,
        ILogger<SeedDataHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(workItemStore);
        ArgumentNullException.ThrowIfNull(calendarEventStore);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _workItemStore = workItemStore;
        _calendarEventStore = calendarEventStore;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            Log.SeedDataDisabled(_logger);
            return;
        }

        var now = DateTimeOffset.UtcNow;

        await SeedTeamsMessagesAsync(now, cancellationToken);
        await SeedOutlookEmailsAsync(now, cancellationToken);
        await SeedCalendarEventsAsync(now, cancellationToken);
        await SeedPullRequestsAsync(now, cancellationToken);

        Log.SeedDataCompleted(_logger);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedTeamsMessagesAsync(DateTimeOffset now, CancellationToken ct)
    {
        var teamsMessages = new[]
        {
            CreateTeamsWorkItem(
                "teams-seed-001", "Incidente de producción en API Payments", WorkItemPriority.Critical,
                "Carlos Ruiz", "El endpoint POST /payments está devolviendo 502 desde las 14:30. Todos los cobros están fallando. Necesito que alguien revise ya.",
                "team-payments", "channel-incidents", now.AddMinutes(-15)),
            CreateTeamsWorkItem(
                "teams-seed-002", "Release v3.2.1 desplegada mañana a las 10", WorkItemPriority.High,
                "Ana López", "Mañana desplegamos la release v3.2.1 con las nuevas features de reporting. Por favor confirmad que vuestros PRs están merged.",
                "team-core", "channel-releases", now.AddMinutes(-45)),
            CreateTeamsWorkItem(
                "teams-seed-003", "Duda sobre el endpoint /users/:id/roles", WorkItemPriority.Medium,
                "Pedro Gómez", "Alguien sabe si /users/:id/roles acepta PATCH o solo PUT? No lo veo en la doc de Swagger.",
                "team-core", "channel-dev", now.AddHours(-2)),
            CreateTeamsWorkItem(
                "teams-seed-004", "URGENTE: Rollback necesario en producción", WorkItemPriority.Critical,
                "María García", "La última deploy rompió la validación de emails. Hay que hacer rollback AHORA. @on-call",
                "team-platform", "channel-ops", now.AddMinutes(-5)),
            CreateTeamsWorkItem(
                "teams-seed-005", "Propuesta de arquitectura para el módulo de reports", WorkItemPriority.Low,
                "Laura Sánchez", "Os comparto el documento con la propuesta de arquitectura para el nuevo módulo de reports. Comentarios bienvenidos.",
                "team-core", "channel-architecture", now.AddHours(-5)),
            CreateTeamsWorkItem(
                "teams-seed-006", "Daily standup en 5 minutos", WorkItemPriority.Medium,
                "Sistema", "Recordatorio automático: Daily standup en 5 minutos. Sala virtual: https://teams.aura.dev/daily",
                null, null, now.AddMinutes(-10)),
            CreateTeamsWorkItem(
                "teams-seed-007", "Bug crítico en el flujo de login con SSO", WorkItemPriority.High,
                "David Martínez", "Los usuarios de Okta no pueden iniciar sesión desde esta mañana. El redirect_uri no coincide con lo configurado.",
                "team-auth", "channel-security", now.AddMinutes(-30))
        };

        foreach (var item in teamsMessages)
        {
            await _workItemStore.SaveAsync(item, ct);
        }

        Log.SeedDataInserted(_logger, "Teams", teamsMessages.Length);
    }

    private async Task SeedOutlookEmailsAsync(DateTimeOffset now, CancellationToken ct)
    {
        var outlookEmails = new[]
        {
            CreateOutlookWorkItem(
                "outlook-seed-001", "URGENTE: Producción caída — responder inmediatamente", WorkItemPriority.Critical,
                "ceo@aura.dev", "La página principal está caída según nuestros clientes. Necesito una actualización cada 15 minutos hasta que se resuelva. Por favor responder con ETA.",
                "conv-prod-001", now.AddMinutes(-20), "high", true),
            CreateOutlookWorkItem(
                "outlook-seed-002", "Review del diseño de base de datos para el nuevo módulo", WorkItemPriority.High,
                "director@aura.dev", "Adjunto el diseño preliminar de la BD para el módulo de auditoría. Necesito vuestra review antes del viernes. Prestad atención a los índices propuestos.",
                "conv-db-review", now.AddMinutes(-90), "normal", false),
            CreateOutlookWorkItem(
                "outlook-seed-003", "Weekly status - Semana 25", WorkItemPriority.Medium,
                "manager@aura.dev", "Por favor enviad vuestro weekly status antes de las 15:00. Incluid logros, bloqueos y planes para la próxima semana.",
                "conv-weekly", now.AddHours(-3), "normal", true),
            CreateOutlookWorkItem(
                "outlook-seed-004", "Re: Presupuesto Q3 aprobado — fondos disponibles", WorkItemPriority.Medium,
                "finanzas@aura.dev", "El presupuesto para Q3 ha sido aprobado. Tenemos $50k para infraestructura y $30k para herramientas. Enviad vuestras solicitudes antes del 15 de julio.",
                "conv-budget", now.AddHours(-1)), 
            CreateOutlookWorkItem(
                "outlook-seed-005", "Incidente de seguridad reportado por cliente SOC-2", WorkItemPriority.Critical,
                "vp-seguridad@aura.dev", "Un cliente ha reportado una posible brecha de seguridad en su instancia. El equipo de seguridad ya está investigando. No compartáis información externa hasta nuevo aviso.",
                "conv-sec-incident", now.AddMinutes(-40), "high", true),
            CreateOutlookWorkItem(
                "outlook-seed-006", "Invitación: Hackathon interno de innovación", WorkItemPriority.Low,
                "cultura@aura.dev", "Os invitamos al hackathon interno del 20-21 de julio. 24h para construir algo genial. Habrá premios y comida gratis! Inscribíos en el portal.",
                "conv-hackathon", now.AddHours(-6)),
            CreateOutlookWorkItem(
                "outlook-seed-007", "ASAP: Hotfix para producción esta tarde", WorkItemPriority.High,
                "cto@aura.dev", "Necesito un hotfix para el bug de pagos antes de las 18:00. El CEO ha puesto este tema como prioritario. Avísame cuando tengáis el PR listo para revisarlo yo mismo.",
                "conv-hotfix", now.AddMinutes(-60), "high", true)
        };

        foreach (var item in outlookEmails)
        {
            await _workItemStore.SaveAsync(item, ct);
        }

        Log.SeedDataInserted(_logger, "Outlook", outlookEmails.Length);
    }

    private async Task SeedCalendarEventsAsync(DateTimeOffset now, CancellationToken ct)
    {
        var events = new[]
        {
            new CalendarEvent(
                Id: "cal-seed-001",
                Title: "Sprint Planning — Sprint 12",
                StartUtc: now.AddHours(1),
                EndUtc: now.AddHours(2),
                IsOnlineMeeting: true,
                JoinUrl: "https://teams.microsoft.com/l/meetup-join/sprint12",
                Organizer: "Ana López",
                Location: "Teams — Sala Principal",
                OriginalTimeZone: "America/Mexico_City"),
            new CalendarEvent(
                Id: "cal-seed-002",
                Title: "1:1 con Manager",
                StartUtc: now.AddHours(3),
                EndUtc: now.AddHours(3).AddMinutes(30),
                IsOnlineMeeting: true,
                JoinUrl: "https://teams.microsoft.com/l/meetup-join/oneone",
                Organizer: "María García",
                Location: "Teams — Sala Manager",
                OriginalTimeZone: "America/Mexico_City"),
            new CalendarEvent(
                Id: "cal-seed-003",
                Title: "Daily Standup",
                StartUtc: now.AddMinutes(30),
                EndUtc: now.AddMinutes(45),
                IsOnlineMeeting: false,
                Location: "Sala B — Piso 3"),
            new CalendarEvent(
                Id: "cal-seed-004",
                Title: "Design Review — Nuevo Dashboard",
                StartUtc: now.AddHours(5),
                EndUtc: now.AddHours(6),
                IsOnlineMeeting: true,
                JoinUrl: "https://teams.microsoft.com/l/meetup-join/designreview",
                Organizer: "Laura Sánchez",
                Location: "Teams — Sala Diseño",
                OriginalTimeZone: "America/Mexico_City"),
            new CalendarEvent(
                Id: "cal-seed-005",
                Title: "Code Review Session — Feature branches",
                StartUtc: now.AddHours(7),
                EndUtc: now.AddHours(7).AddMinutes(45),
                IsOnlineMeeting: true,
                JoinUrl: "https://teams.microsoft.com/l/meetup-join/codereview",
                Organizer: "David Martínez",
                Location: "Teams — Sala Dev"),
            new CalendarEvent(
                Id: "cal-seed-006",
                Title: "Almuerzo con equipo",
                StartUtc: now.AddHours(4),
                EndUtc: now.AddHours(5),
                IsOnlineMeeting: false,
                Location: "Comedor — Piso 2"),
        };

        await _calendarEventStore.SaveBatchAsync(events, ct);
        Log.SeedDataInserted(_logger, "Calendar", events.Length);
    }

    private async Task SeedPullRequestsAsync(DateTimeOffset now, CancellationToken ct)
    {
        var prItems = new[]
        {
            CreatePrWorkItem(
                "pr-seed-001", "Hotfix: production crash on payment validation", WorkItemPriority.Critical,
                "Carlos Ruiz", "Aura", 12, 3, "active", false, now.AddMinutes(-30)),
            CreatePrWorkItem(
                "pr-seed-002", "Fix: SSO redirect loop on token expiry", WorkItemPriority.Critical,
                "David Martínez", "Aura.Auth", 8, 5, "active", false, now.AddMinutes(-90)),
            CreatePrWorkItem(
                "pr-seed-003", "Feature: Add reporting dashboard v2", WorkItemPriority.High,
                "Laura Sánchez", "Aura", 5, 12, "active", false, now.AddHours(-2)),
            CreatePrWorkItem(
                "pr-seed-004", "Refactor: Extract payment gateway adapter", WorkItemPriority.High,
                "Pedro Gómez", "Aura.Payments", 3, 8, "active", false, now.AddHours(-4)),
            CreatePrWorkItem(
                "pr-seed-005", "Chore: Update dependency versions", WorkItemPriority.Low,
                "Sistema", "Aura", 0, 15, "active", true, now.AddHours(-1)),
            CreatePrWorkItem(
                "pr-seed-006", "Docs: Update API reference for v3 endpoints", WorkItemPriority.Low,
                "María García", "Aura.Docs", 1, 4, "active", false, now.AddHours(-3))
        };

        foreach (var item in prItems)
        {
            await _workItemStore.SaveAsync(item, ct);
        }

        Log.SeedDataInserted(_logger, "PrReview", prItems.Length);
    }

    private static WorkItem CreatePrWorkItem(
        string externalId,
        string title,
        WorkItemPriority priority,
        string author,
        string repo,
        int commentCount,
        int fileCount,
        string prStatus,
        bool isDraft,
        DateTimeOffset capturedAt)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["pr.pullRequestId"] = externalId.Replace("pr-seed-", ""),
            ["pr.status"] = prStatus,
            ["pr.repo"] = repo,
            ["pr.author"] = author,
            ["pr.reviewers"] = author == "Carlos Ruiz" ? "Ana López,Pedro Gómez"
                : author == "David Martínez" ? "María García"
                : author == "Laura Sánchez" ? "Ana López,Carlos Ruiz,Pedro Gómez"
                : author == "Pedro Gómez" ? "Laura Sánchez"
                : author == "María García" ? "Carlos Ruiz"
                : "",
            ["pr.reviewerCount"] = author switch
            {
                "Carlos Ruiz" => "2",
                "David Martínez" => "1",
                "Laura Sánchez" => "3",
                "Pedro Gómez" => "1",
                "María García" => "1",
                _ => "0"
            },
            ["pr.commentCount"] = commentCount.ToString(),
            ["pr.fileCount"] = fileCount.ToString(),
            ["pr.isDraft"] = isDraft.ToString(),
            ["pr.sourceLink"] = $"https://dev.azure.com/auraorg/Aura/_git/{repo}/pullrequest/{externalId.Replace("pr-seed-", "")}",
            ["pr.priority.raw"] = priority.ToString(),
            ["pr.priority.resolution"] = "explicit"
        };

        return new WorkItem(
            externalId: externalId,
            title: title,
            source: "pr",
            sourceType: WorkItemSourceType.PrReview,
            priority: priority,
            metadata: metadata,
            correlationId: $"corr-{externalId}",
            capturedAtUtc: capturedAt);
    }

    private static WorkItem CreateTeamsWorkItem(
        string externalId,
        string title,
        WorkItemPriority priority,
        string sender,
        string body,
        string? teamId,
        string? channelId,
        DateTimeOffset capturedAt)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["teams.sender"] = sender,
            ["teams.snippet"] = body,
            ["teams.deepLink"] = $"https://teams.microsoft.com/l/message/{externalId}",
            ["teams.teamId"] = teamId ?? "general",
            ["teams.channelId"] = channelId ?? "general",
            ["teams.messageUrl"] = $"https://teams.microsoft.com/l/message/{externalId}",
            ["teams.priority.raw"] = priority.ToString(),
            ["teams.priority.resolution"] = "explicit"
        };

        return new WorkItem(
            externalId: externalId,
            title: title,
            source: "messages",
            sourceType: WorkItemSourceType.TeamsMessage,
            priority: priority,
            metadata: metadata,
            correlationId: $"corr-{externalId}",
            capturedAtUtc: capturedAt);
    }

    private static WorkItem CreateOutlookWorkItem(
        string externalId,
        string subject,
        WorkItemPriority priority,
        string senderAddress,
        string bodyPreview,
        string conversationId,
        DateTimeOffset receivedAt,
        string? importance = null,
        bool hasDeadline = false)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["outlook.sender"] = senderAddress,
            ["outlook.snippet"] = bodyPreview,
            ["outlook.deepLink"] = $"https://outlook.office.com/mail/{externalId}",
            ["outlook.conversationId"] = conversationId,
            ["outlook.importance.raw"] = importance ?? "normal",
            ["outlook.scoring.totalScore"] = priority switch
            {
                WorkItemPriority.Critical => "7",
                WorkItemPriority.High => "4",
                WorkItemPriority.Medium => "2",
                _ => "0"
            }
        };

        if (hasDeadline)
        {
            metadata["outlook.deadline.cue"] = "by eod";
            metadata["outlook.deadline.source"] = "body";
        }

        return new WorkItem(
            externalId: externalId,
            title: subject,
            source: "inbox",
            sourceType: WorkItemSourceType.OutlookEmail,
            priority: priority,
            metadata: metadata,
            correlationId: $"corr-{externalId}",
            capturedAtUtc: receivedAt);
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 9001, Level = LogLevel.Information,
            Message = "SeedData: Inserted {Count} {Source} items")]
        public static partial void SeedDataInserted(ILogger logger, string source, int count);

        [LoggerMessage(EventId = 9002, Level = LogLevel.Information,
            Message = "SeedData: Seeding completed successfully")]
        public static partial void SeedDataCompleted(ILogger logger);

        [LoggerMessage(EventId = 9003, Level = LogLevel.Information,
            Message = "SeedData: Disabled via configuration — skipping seed")]
        public static partial void SeedDataDisabled(ILogger logger);
    }
}
