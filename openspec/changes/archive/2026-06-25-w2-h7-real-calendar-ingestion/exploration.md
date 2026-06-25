# Exploration: W2-H7 — Real Calendar Ingestion

**Change:** `w2-h7-real-calendar-ingestion`
**Date:** 2026-06-24
**Artifact store:** openspec
**Status:** Ready for Proposal

---

## Current State

### What W2-H8 Already Built (Reusable Foundation)

W2-H8 (real Teams/Outlook ingestion) is fully implemented across 3 PRs. The following infrastructure is **directly reusable** for calendar:

| Component | Path | Reuse for Calendar |
|-----------|------|--------------------|
| `IGraphClientFactory` | `Infrastructure/Adapters/Connectors/Graph/IGraphClientFactory.cs` | ✅ Same `GraphServiceClient` creation pattern; calendar provider injects same factory |
| `GraphClientFactory` | `Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` | ✅ Delegated token acquisition via MSAL `AcquireTokenSilent`; only scopes change |
| `MsalSqliteTokenCache` | `Infrastructure/Adapters/Connectors/Graph/MsalSqliteTokenCache.cs` | ✅ SQLite token cache already wired; no changes needed |
| `GraphConnectorOptions` | `Infrastructure/Adapters/GraphConnector/GraphConnectorOptions.cs` | ✅ Already has `Scopes[]` array; add `Calendars.Read` scope |
| `Graph/DependencyInjection.cs` | `Infrastructure/Adapters/Connectors/Graph/DependencyInjection.cs` | ✅ MSAL + SQLite + GraphServiceClient registration already complete |
| `ConnectorExecutionWorker` | `Workers/ConnectorExecutionWorker.cs` | ✅ Already iterates all `IConnectorAdapter` registrations; calendar adapter auto-picked up |
| `TriggerSyncUseCase` | `Application/UseCases/IngestionSync/TriggerSyncUseCase.cs` | ✅ Already aggregates per-source sync results; calendar adds a source |
| `SyncEndpoints` | `Api/Endpoints/SyncEndpoints.cs` | ✅ `POST /api/sync/now` + `GET /api/sync/status` already exist |
| `IConnectorAdapter` pattern | `Application/Ports/IConnectorAdapter.cs` | ✅ Calendar adapter implements same interface |
| `IMessageSourceProvider<T>` pattern | `Application/Ports/IMessageSourceProvider.cs` | ✅ Calendar source provider follows same generic pattern |

### What's Unique to Calendar (NOT in W2-H8)

The architecture doc at `docs/architecture/ingestion/03-microsoft-graph-calendar.md` specifies a **separate domain** from WorkItem ingestion:

> "Los `CalendarEvent` NO son `WorkItem`. No entran al triage ni al pipeline de ingestión."

This is a critical architectural distinction. Calendar is **not** a WorkItem source — it's its own domain with:

| Concept | Calendar Domain | WorkItem Ingestion (W2-H8) |
|---------|----------------|---------------------------|
| Domain model | `CalendarEvent` (Id, Title, StartUtc, EndUtc, IsOnlineMeeting, JoinUrl?) | `WorkItem` (ExternalId, Title, Source, SourceType, Priority, Metadata) |
| Storage | `aura.db` SQLite table `calendar_events` (or in-memory for first slice) | `SqliteWorkItemStore` (WorkItems table) |
| Use case | `GetUpcomingMeetingsUseCase` — dashboard display only | `ExecuteConnectorUseCase` — pipeline processing |
| Alert system | `MeetingAlertWorker` + `SignalRMeetingAlertDispatcher` | N/A |
| Port | `ICalendarEventProvider` | `IConnectorAdapter` + `IMessageSourceProvider<T>` |
| UI | `UpcomingMeetingsPanel.razor` (separate panel) | `InboxPreviewPanel.razor` |

### Existing Calendar-Aware Code

- `WorkItemSourceType.CalendarAppointment` — already defined in Domain enum (line 11 of `WorkItemSourceType.cs`)
- No `ICalendarEventProvider` implementation exists yet
- No `CalendarEvent` domain model exists yet
- No `UpcomingMeetingsPanel.razor` exists yet
- No Graph calendar endpoint calls exist anywhere in the codebase

---

## Affected Areas

### New files to create

| File | Purpose |
|------|---------|
| `src/Aura.Domain/Calendar/CalendarEvent.cs` | Domain model: Id, Title, StartUtc, EndUtc, IsOnlineMeeting, JoinUrl?, Organizer?, Location? |
| `src/Aura.Domain/Calendar/MeetingAlertTrigger.cs` | Enum: SixtyMinutes, TenMinutes, FiveMinutes |
| `src/Aura.Domain/Calendar/MeetingAlert.cs` | Domain model: EventId, Title, Trigger, StartsAtUtc, JoinUrl? |
| `src/Aura.Application/Ports/ICalendarEventProvider.cs` | Port: `GetEventsAsync(DateTimeOffset date, string userId, CancellationToken)` → `IReadOnlyList<CalendarEvent>` |
| `src/Aura.Application/Ports/IMeetingAlertStore.cs` | Port: `HasBeenSentAsync` / `MarkSentAsync` — idempotency |
| `src/Aura.Application/Ports/IMeetingAlertDispatcher.cs` | Port: `DispatchAsync(alert)` — SignalR output |
| `src/Aura.Application/UseCases/Calendar/GetUpcomingMeetingsUseCase.cs` | Dashboard use case |
| `src/Aura.Application/UseCases/Calendar/CheckAndDispatchMeetingAlertsUseCase.cs` | Worker use case |
| `src/Aura.Infrastructure/Adapters/Calendar/GraphCalendarEventProvider.cs` | `ICalendarEventProvider` impl via `/me/calendarView` |
| `src/Aura.Infrastructure/Adapters/Calendar/SqliteMeetingAlertStore.cs` | `IMeetingAlertStore` impl in `aura.db` |
| `src/Aura.Infrastructure/Adapters/Calendar/SignalRMeetingAlertDispatcher.cs` | `IMeetingAlertDispatcher` impl |
| `src/Aura.Infrastructure/Adapters/Calendar/DependencyInjection.cs` | Calendar DI registration |
| `src/Aura.Api/Hubs/MeetingAlertHub.cs` | SignalR hub with groups by UserId |
| `src/Aura.Workers/MeetingAlertWorker.cs` | Polling worker (every 2 min) |
| `src/Aura.UI/Components/Dashboard/UpcomingMeetingsPanel.razor` | Dashboard panel |
| `src/Aura.UI/Components/Dashboard/MeetingAlertToast.razor` | Toast notification component |
| `src/Aura.UI/wwwroot/js/meetingAlert.js` | Web Notification API + Audio |

### Existing files to modify

| File | Change |
|------|--------|
| `src/Aura.Infrastructure/Adapters/Connectors/Graph/GraphClientFactory.cs` | Scopes already configurable via `GraphConnectorOptions.Scopes[]` — just add `Calendars.Read` to User Secrets config. No code change needed. |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Wire calendar sub-registration (`services.AddCalendar(...)`) |
| `src/Aura.Api/Program.cs` | Map SignalR hub (`app.MapHub<MeetingAlertHub>("/hubs/meeting-alerts")`) |
| `src/Aura.UI/Pages/Index.razor` | Add `<UpcomingMeetingsPanel />` and `<MeetingAlertToast />` |
| `openspec/specs/dashboard-inbox-preview/spec.md` | Delta: calendar is separate panel, not part of inbox preview |

---

## Graph API Calendar Endpoints

| Endpoint | Method | Scope | Returns | Notes |
|----------|--------|-------|---------|-------|
| `GET /me/calendarView?startDateTime=...&endDateTime=...` | Delegated | `Calendars.Read` | Events for time window | Preferred for delegated flow — respects user's default calendar |
| `GET /me/events?$filter=start/dateTime ge '...' and start/dateTime le '...'` | Delegated | `Calendars.Read` | Events collection | Alternative; less precise for time zones |
| `GET /me/calendars/{id}/calendarView` | Delegated | `Calendars.Read` | Specific calendar events | For non-default calendars |
| `GET /me/calendarGroups/{id}/calendars/{id}/calendarView` | Delegated | `Calendars.Read` | Grouped calendar events | Overkill for first slice |

**Recommended for first slice:** `GET /me/calendarView` with `startDateTime` and `endDateTime` parameters. This is the standard approach for delegated flow and respects the user's default calendar.

### Graph Calendar Event Response Shape (relevant fields)

```json
{
  "id": "AAMkAGI2...",
  "subject": "Team standup",
  "start": { "dateTime": "2026-06-24T09:00:00.0000000", "timeZone": "UTC" },
  "end": { "dateTime": "2026-06-24T09:30:00.0000000", "timeZone": "UTC" },
  "isOnlineMeeting": true,
  "onlineMeeting": { "joinUrl": "https://teams.microsoft.com/l/meetup-join/..." },
  "organizer": { "emailAddress": { "name": "John", "address": "john@contoso.com" } },
  "location": { "displayName": "Teams Meeting" },
  "isCancelled": false,
  "showAs": "busy"
}
```

---

## Scopes Required

| Scope | Type | Purpose | Consent |
|-------|------|---------|---------|
| `Calendars.Read` | Delegated | Read user's calendar events | Incremental consent on first calendar access |
| `Calendars.ReadWrite` | Delegated | (Future) Mark events as read/respond | NOT needed for first slice |
| `offline_access` | Delegated | Refresh tokens for delegated flow | Already granted by W2-H8 |
| `openid profile` | Delegated | Identity context | Already granted by W2-H8 |

**Incremental consent strategy:** W2-H8 already established delegated-first auth with `Mail.Read`, `Chat.Read`, `User.Read`. Calendar adds `Calendars.Read` as an additional scope. The existing `GraphConnectorOptions.Scopes[]` array in User Secrets controls which scopes are requested. Adding `Calendars.Read` to that array is a config change, not a code change.

**Key decision from architecture doc:** The architecture doc says `Calendars.Read` with **admin consent** using `ClientSecretCredential` (Application permission, not Delegated). However, the user's locked decision is **delegated-first** (same as W2-H8). This means:
- Use `AuthorizationCodeCredential` (delegated) for calendar, same as W2-H8 uses for mail/chat
- `Calendars.Read` delegated scope does NOT require admin consent — it's a user-level consent
- The architecture doc's "admin consent" recommendation may be outdated or referring to a different flow

---

## What Can Be Reused vs. What's New

### Directly Reusable (Zero/Low Effort)

1. **Graph auth infrastructure** — `GraphClientFactory`, `MsalSqliteTokenCache`, `GraphConnectorOptions`, DI registration. Calendar provider just injects `IGraphClientFactory` and calls `CreateClientAsync()`.
2. **`IConnectorAdapter` pattern** — Calendar adapter implements `IConnectorAdapter` with `ConnectorName = "calendar"`. Worker auto-discovers it.
3. **`TriggerSyncUseCase` aggregation** — Calendar sync results join the existing per-source aggregation.
4. **`SyncEndpoints`** — `POST /api/sync/now` and `GET /api/sync/status` already handle multi-source. Calendar adds a source.
5. **`ConnectorExecutionWorker`** — Already iterates `IEnumerable<IConnectorAdapter>`. Calendar adapter registration is all that's needed.
6. **Fixture fallback pattern** — Calendar adapter can use `Func<IReadOnlyList<CalendarEventDto>>` for controlled-demo mode.

### New (Requires Implementation)

1. **Domain model** — `CalendarEvent`, `MeetingAlertTrigger`, `MeetingAlert` in `Aura.Domain/Calendar/`
2. **Ports** — `ICalendarEventProvider`, `IMeetingAlertStore`, `IMeetingAlertDispatcher` in Application
3. **Graph calendar provider** — `GraphCalendarEventProvider` calling `/me/calendarView`
4. **Alert infrastructure** — `SqliteMeetingAlertStore`, `SignalRMeetingAlertDispatcher`, `MeetingAlertHub`
5. **Worker** — `MeetingAlertWorker` (polling every 2 min)
6. **UI** — `UpcomingMeetingsPanel.razor`, `MeetingAlertToast.razor`, `meetingAlert.js`

---

## Architectural Decision: Calendar as Connector vs. Separate Domain

The architecture doc explicitly says Calendar is NOT a WorkItem source. But the user's success criteria says "dashboard loads, shows calendar data, preview works, summary makes sense" — which suggests calendar items should appear in the dashboard.

**Two approaches:**

### Approach A: Calendar as IConnectorAdapter (WorkItem path)

Calendar events flow through `IConnectorAdapter` → `IWorkItemBuffer` → `IWorkItemStore` → `DashboardPreviewReader` → `InboxPreviewPanel`. Calendar events become `WorkItem` with `SourceType = CalendarAppointment`.

| Pros | Cons |
|------|------|
| Zero new UI components needed — events appear in existing inbox panel | Architecture doc explicitly says "CalendarEvent NO son WorkItem" |
| Reuses entire existing pipeline | Calendar events don't need triage/scoring/ranking |
| Dashboard preview shows calendar alongside mail/teams | Loses calendar-specific metadata (start/end time, join URL, duration) |
| Minimal new code | Forces calendar into a square peg |

### Approach B: Separate Calendar Domain (architecture doc path)

Calendar has its own domain model, ports, use case, and UI panel. Completely separate from WorkItem ingestion.

| Pros | Cons |
|------|------|
| Follows architecture doc design | Requires new UI panel, new SignalR hub, new worker |
| Calendar-specific metadata preserved | More code, more test surface |
| Natural for alerts/notifications later | Calendar data doesn't appear in inbox preview |
| Clean separation of concerns | Need separate dashboard section |

### Recommendation: Hybrid — Use IConnectorAdapter for sync, Separate Panel for display

Use the existing `IConnectorAdapter` pipeline for the Graph data fetch + sync (reuses auth, worker, sync endpoints), but display calendar events in a **separate `UpcomingMeetingsPanel.razor`** that reads from a calendar-specific store. This gives:
- Reuse of Graph auth + sync infrastructure
- Calendar-specific UI with start/end time, duration, join URL
- Architecture doc compliance (CalendarEvent is NOT a WorkItem)
- Separate panel on dashboard alongside inbox preview

---

## Risks

1. **Architecture doc says Application permission, user wants Delegated** — The doc says `ClientSecretCredential` + `Calendars.Read` with admin consent. The locked decision is delegated-first. Need to confirm: delegated `Calendars.Read` does NOT require admin consent and works with `AuthorizationCodeCredential`. If the target tenant has admin consent policies that block delegated calendar access, this breaks. **Mitigation:** Test with the actual Entra app registration early.

2. **Calendar events are not WorkItems** — If we use the `IConnectorAdapter` path for sync, calendar events must be stored separately (not in WorkItemStore). Need a `ICalendarEventStore` or similar. This adds a new persistence concern.

3. **Time zone handling** — Graph returns calendar events with timezone info. Converting to UTC correctly is critical for alert timing. The architecture doc flags this as a risk. **Mitigation:** Always normalize to UTC in the adapter, store original timezone in metadata.

4. **SignalR is not yet implemented** — The architecture doc requires SignalR for meeting alerts. If SignalR isn't set up yet, the alert system can't work. **Mitigation:** First slice can skip alerts and just show the panel. Alerts are a follow-up slice.

5. **`Calendars.Read` scope availability** — Delegated `Calendars.Read` is generally available but may be restricted by tenant Conditional Access policies. **Mitigation:** Graceful degradation — if calendar fetch fails, show empty panel with error message.

6. **Multiple calendar support** — First slice should only read the default calendar. Users with shared/delegate calendars may not see all events. **Mitigation:** Document as known limitation.

---

## Ready for Proposal

**Yes.** All architectural patterns are well-understood from W2-H8. The key decision to carry into proposal is:

- **Hybrid approach:** Use `IConnectorAdapter` for Graph fetch + sync reuse, but `CalendarEvent` as a separate domain model displayed in its own panel (`UpcomingMeetingsPanel.razor`).
- **First slice scope:** Calendar events in dashboard panel only. Alerts (60/10/5 min notifications) are a follow-up slice.
- **Auth:** Delegated-first, same `GraphServiceClient` from W2-H8, add `Calendars.Read` to scopes config.
- **Architecture doc note:** The doc says `ClientSecretCredential` + admin consent, but the locked decision is delegated-first. The proposal should explicitly call this out and confirm the delegated approach works.
