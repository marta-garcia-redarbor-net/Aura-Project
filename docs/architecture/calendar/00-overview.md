# Calendar — Architecture Overview

The Calendar bounded context handles upcoming meetings and proactive alerting for the signed-in user.

---

## Core contracts

```text
ICalendarApiClient          ← UI layer HTTP client for /api/calendar endpoints
ICalendarEventStore         ← In-memory event store for calendar data
GetUpcomingMeetingsUseCase  ← Application layer: retrieves and orders upcoming meetings
```

---

## Endpoint

### GET /api/calendar/upcoming

Returns upcoming calendar events for the authenticated user, ordered by `StartUtc` ascending.

**Response DTO: `UpcomingMeetingResponse`**

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `string` | Unique event identifier |
| `Title` | `string` | Event subject/title |
| `StartUtc` | `DateTimeOffset` | Event start time (UTC) |
| `EndUtc` | `DateTimeOffset` | Event end time (UTC) |
| `IsOnlineMeeting` | `bool` | Whether the event has an online meeting link |
| `JoinUrl` | `string?` | Online meeting join URL (if available) |
| `Organizer` | `string?` | Event organizer name/email |
| `Location` | `string?` | Physical or virtual location |

---

## Data flow

```text
Graph Calendar API (delegated user token)
  → Aura.Api endpoints fetch calendar events
  → CalendarApiClient (UI layer) calls GET /api/calendar/upcoming
  → PrioritySummaryService merges with preview data
  → PrioritySummaryCards renders Schedule Today card
```

---

## Priority Dashboard integration

The Schedule Today card in PrioritySummaryCards uses `IPrioritySummaryService` to compose calendar data with Teams/Outlook preview items. Calendar events are ordered by `StartUtc` ascending and displayed with:

- Timeline indicator (past/current/upcoming status colors)
- Time range, title, location
- Online meeting join link (when available)
- Footer with "View all X meetings" + "Open Calendar" link

---

## Related files

- `src/Aura.Api/Endpoints/DashboardEndpoints.cs` — Calendar API endpoint
- `src/Aura.UI/Services/ICalendarApiClient.cs` — UI-layer HTTP client interface
- `src/Aura.UI/Services/CalendarApiClient.cs` — HTTP client implementation (telemetry + resilience)
- `src/Aura.UI/Services/IPrioritySummaryService.cs` — Composes preview + calendar data
- `src/Aura.UI/Services/PrioritySummaryService.cs` — Service implementation (telemetry)
- `src/Aura.UI/Components/Dashboard/PrioritySummaryCards.razor` — Renders source-based cards
- `src/Aura.UI/Models/UpcomingMeetingResponse.cs` — Calendar response DTO

---

## Telemetry and resilience

| Component | ActivitySource | Resilience |
|-----------|---------------|------------|
| `CalendarApiClient` | `Aura.UI.CalendarApi` | `AddStandardResilienceHandler()` (timeout, retry with jitter, circuit breaker) |
| `PrioritySummaryService` | `Aura.UI.PrioritySummary` | N/A (orchestration layer) |

Both components emit structured logs via `ILogger` and activity tags for OpenTelemetry correlation.
