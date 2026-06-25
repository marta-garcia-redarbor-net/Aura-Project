# Proposal: W2-H7 — Real Calendar Ingestion

## Intent

The dashboard currently shows inbox previews and morning summary, but has no visibility into the user's calendar. Engineers waste time switching between Outlook/Teams and Aura to check upcoming meetings. This change adds real Microsoft Graph calendar ingestion so the dashboard displays live upcoming meetings with duration, join URLs, and organizer context.

## Scope

### In Scope
- `CalendarEvent` domain model (Id, Title, StartUtc, EndUtc, IsOnlineMeeting, JoinUrl?, Organizer?, Location?)
- `ICalendarEventProvider` port + `GraphCalendarEventProvider` adapter calling `GET /me/calendarView`
- `GetUpcomingMeetingsUseCase` — fetches events for today + configurable window
- `UpcomingMeetingsPanel.razor` — separate Blazor panel on dashboard (not inside InboxPreviewPanel)
- Reuse W2-H8 `IGraphClientFactory` + `GraphClientFactory` + MSAL token cache
- Add `Calendars.Read` to `GraphConnectorOptions.Scopes[]` (config, not code)
- `ConnectorExecutionWorker` auto-discovers calendar adapter via `IConnectorAdapter`
- Fixture fallback for controlled-demo mode
- Unit + integration tests

### Out of Scope
- Meeting alerts/notifications (60/10/5 min) — follow-up slice
- `SignalRMeetingAlertDispatcher`, `MeetingAlertHub`, `MeetingAlertWorker`
- `MeetingAlertToast.razor`, `meetingAlert.js`
- `Calendars.ReadWrite` scope
- Multiple calendar support (only default calendar)
- Event creation, modification, or RSVP

## Capabilities

### New Capabilities
- `calendar-ingestion`: Domain model, Graph provider port, adapter, and use case for fetching and displaying calendar events

### Modified Capabilities
- `dashboard-inbox-preview`: Calendar panel is a separate component alongside InboxPreviewPanel; no changes to inbox preview requirements, but dashboard layout gains a new panel section

## Approach

**Hybrid architecture:** Use existing `IConnectorAdapter` pipeline for Graph data fetch + sync (reuses auth, worker, sync endpoints), but store and display calendar events via a **separate domain model** (`CalendarEvent`) in a dedicated `UpcomingMeetingsPanel.razor`.

Key decisions:
1. **Delegated-first auth** — same `GraphServiceClient` from W2-H8, add `Calendars.Read` to scopes config. Architecture doc says `ClientSecretCredential` + admin consent, but locked decision is delegated-first.
2. **Incremental consent** — `Calendars.Read` added to existing scopes array; user consents on first calendar access.
3. **First slice** — calendar events in dashboard panel only. Alerts are deferred.
4. **Graph endpoint** — `GET /me/calendarView?startDateTime=...&endDateTime=...` with UTC-normalized storage.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `src/Aura.Domain/Calendar/` | New | `CalendarEvent` domain model |
| `src/Aura.Application/Ports/ICalendarEventProvider.cs` | New | Port for calendar event retrieval |
| `src/Aura.Application/UseCases/Calendar/` | New | `GetUpcomingMeetingsUseCase` |
| `src/Aura.Infrastructure/Adapters/Calendar/` | New | `GraphCalendarEventProvider`, DI registration |
| `src/Aura.UI/Components/Dashboard/UpcomingMeetingsPanel.razor` | New | Dashboard calendar panel |
| `src/Aura.Infrastructure/DependencyInjection.cs` | Modified | Wire calendar sub-registration |
| `src/Aura.UI/Pages/Index.razor` | Modified | Add `<UpcomingMeetingsPanel />` |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Delegated `Calendars.Read` blocked by tenant Conditional Access | Med | Graceful degradation — empty panel with error message; test with actual Entra app early |
| Time zone conversion errors | Med | Normalize to UTC in adapter; store original timezone in metadata |
| Calendar events incorrectly modeled as WorkItems | Low | Explicit `CalendarEvent` domain model; architecture doc compliance |
| Graph calendar endpoint rate limits | Low | Use standard `/me/calendarView`; cache results within sync window |

## Rollback Plan

1. Remove `UpcomingMeetingsPanel` from `Index.razor` — dashboard reverts to inbox-only view
2. Remove calendar DI registration from `DependencyInjection.cs`
3. Remove `Calendar/` directories from Domain, Application, Infrastructure
4. Remove `Calendars.Read` from scopes config in User Secrets
5. No data migration needed — no persistent calendar storage in first slice

## Dependencies

- W2-H8 complete (Graph auth infrastructure, `IGraphClientFactory`, `GraphClientFactory`, MSAL cache) — **already implemented**
- `GraphConnectorOptions.Scopes[]` array in User Secrets — **already exists**

## Success Criteria

- [ ] Dashboard loads and shows upcoming meetings in a dedicated panel
- [ ] Calendar events display with title, start/end time, duration, and join URL (if online)
- [ ] Preview works with real calendar events from the user's Microsoft 365 account
- [ ] Empty state renders correctly when no meetings exist
- [ ] Error state renders when calendar access fails (permissions, network)
- [ ] `dotnet test Aura.sln` passes with new unit + integration tests
- [ ] Architecture tests confirm no CalendarEvent leakage into WorkItem pipeline
