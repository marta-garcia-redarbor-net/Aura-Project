# Calendar — Meetings and notifications

Calendar is not a triage-ingestion work-item stream. It is a separate bounded context focused on showing the signed-in user's meetings and warning before they start.

## Quick path

1. `MeetingAlertWorker` polls on a schedule.
2. `CheckAndDispatchMeetingAlertsUseCase` loads the user's meetings through `ICalendarEventProvider`.
3. Due alerts are marked in SQLite before dispatch.
4. `SignalRMeetingAlertDispatcher` pushes alerts through the API SignalR host.
5. The UI shows a toast, browser notification, and audio cue.

## Core decisions

| Topic | Decision |
|-------|----------|
| Domain boundary | Calendar is its own bounded context |
| Triage relationship | `CalendarEvent` is not a `WorkItem` |
| Graph access | Use delegated Microsoft Graph user tokens |
| Identity | Use the token `oid` to correlate calendar data and notifications per user |
| Alert persistence | Store sent-alert state in shared SQLite |
| Real-time delivery | Use SignalR between `Aura.Api` and `Aura.UI` |

## Domain model

```text
CalendarEvent        — Id, Title, StartUtc, EndUtc, IsOnlineMeeting, JoinUrl?
MeetingAlertTrigger  — SixtyMinutes | TenMinutes | FiveMinutes
MeetingAlert         — EventId, Title, Trigger, StartsAtUtc, JoinUrl?
```

## Application ports

| Port | Responsibility |
|------|----------------|
| `ICalendarEventProvider` | `GetEventsAsync(date, userId)` returns the user's meetings for the day |
| `IMeetingAlertStore` | `HasBeenSentAsync` / `MarkSentAsync` for idempotency |
| `IMeetingAlertDispatcher` | `DispatchAsync(alert)` sends alerts to SignalR |

## Application use cases

| Use case | Responsibility |
|----------|----------------|
| `GetUpcomingMeetingsUseCase` | Returns upcoming meetings for the dashboard |
| `CheckAndDispatchMeetingAlertsUseCase` | Detects due alerts and dispatches them |

## Infrastructure adapters

| Adapter | Path | Detail |
|---------|------|--------|
| `GraphCalendarEventProvider` | `Adapters/Calendar/` | Calls `/me/calendarView` through `GraphServiceClient` |
| `SqliteMeetingAlertStore` | `Adapters/Calendar/` | Uses a SQLite table for alert idempotency |
| `SignalRMeetingAlertDispatcher` | `Aura.Api/Adapters/` | Pushes alerts to the `MeetingAlertHub` |
| `GraphClientFactory` | `Adapters/Connectors/Graph/` | Creates delegated Graph clients |

## Graph authentication

- Calendar uses **delegated** Microsoft Graph permissions.
- The signed-in user grants Aura access according to tenant consent policy.
- Required scopes are configured as delegated scopes in the Entra ID App Registration.
- A `ClientSecret` is **not required** for the delegated Graph flow documented for Aura.
- Token lifecycle behavior follows the shared auth model: persistent SQLite token cache, silent renewal through MSAL, and re-authentication when silent renewal fails.
- Worker-side calendar processing reuses the delegated token cache; it does not switch to app-only Graph credentials.

## Current runtime limitation

Aura's current Calendar validation path assumes the signed-in user is a **work or school account in the same tenant** and has a real **Exchange Online mailbox/calendar**.

Observed behavior during validation:

- A **personal Microsoft account invited as a guest** can authenticate to Aura and reach the delegated/OBO token path.
- However, `GET /me/calendarView` can still fail because the guest object in the resource tenant does **not** own a tenant mailbox/calendar.
- Therefore, a guest/personal invited account is **not a valid test identity** for the current `/me/calendarView` flow.

Validation rule for real-user calendar sync:

- Use a **tenant-local work/school user**.
- Ensure the user has an **Exchange Online mailbox**.

## SignalR hub

```text
Aura.Api/Hubs/MeetingAlertHub.cs
  — user-scoped delivery groups
  — acknowledgment path for browser-tab coordination
```

## Multi-tab behavior

```text
Worker detects a due alert
  → IMeetingAlertStore.MarkSentAsync()      ← write before dispatch
  → IMeetingAlertDispatcher.DispatchAsync()
      → SignalR → all user tabs
          → browser notification + audio
          → one tab acknowledges
          → other tabs suppress duplicate UX
```

`IMeetingAlertStore` remains the real idempotency boundary. If the alert has already been marked as sent, the worker must not dispatch it again on the next polling cycle.

If delegated token renewal cannot complete silently, calendar features must pause behind re-authentication rather than presenting a second Graph auth model.

## UI responsibilities

| Component | Responsibility |
|-----------|----------------|
| `UpcomingMeetingsPanel.razor` | Shows the signed-in user's meetings for the day |
| `MeetingAlertToast.razor` | Receives SignalR pushes and triggers notification UX |
| `wwwroot/js/meetingAlert.js` | Browser notification and audio behavior |
