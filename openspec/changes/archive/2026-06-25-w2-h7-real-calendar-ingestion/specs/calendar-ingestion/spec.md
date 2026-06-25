# Calendar Ingestion Specification

## Purpose

Domain model, Graph provider port, adapter, and use case for fetching and displaying
Microsoft 365 calendar events. Calendar events are NOT WorkItems — they have their own
domain model, storage, and UI panel. The calendar adapter reuses the existing
`IConnectorAdapter` pipeline for Graph auth, sync, and worker orchestration.

## Requirements

| # | Requirement | Strength |
|---|-------------|----------|
| 1 | Calendar Event Domain Model | MUST |
| 2 | Calendar Connector Adapter | MUST |
| 3 | Upcoming Meetings Use Case | MUST |
| 4 | Dashboard Calendar Panel | MUST |
| 5 | Fixture Fallback | SHOULD |

---

### Requirement: Calendar Event Domain Model

The system MUST define a `CalendarEvent` domain model with fields: Id, Title, StartUtc,
EndUtc, IsOnlineMeeting, JoinUrl (nullable), Organizer (nullable), and Location (nullable).
All datetime fields MUST be stored in UTC. The model MUST NOT inherit from or reference
`WorkItem`.

#### Scenario: Valid calendar event has all required fields

- GIVEN a Microsoft Graph calendar event response with all fields present
- WHEN the adapter maps the response to `CalendarEvent`
- THEN Id, Title, StartUtc, EndUtc, IsOnlineMeeting, JoinUrl, Organizer, and Location are populated

#### Scenario: Online meeting with join URL

- GIVEN a calendar event where `isOnlineMeeting = true`
- WHEN the event is mapped
- THEN `IsOnlineMeeting` is true and `JoinUrl` contains the Teams meeting join URL

#### Scenario: All-day event without online meeting

- GIVEN a calendar event with no online meeting and no organizer
- WHEN the event is mapped
- THEN `IsOnlineMeeting` is false and `JoinUrl` and `Organizer` are null

---

### Requirement: Calendar Connector Adapter

The system MUST implement an `IConnectorAdapter` named `"calendar"` that fetches events
from `GET /me/calendarView` using the existing `IGraphClientFactory`. The adapter MUST
reuse the same `GraphServiceClient` creation pattern as Teams/Outlook adapters. Scope
`Calendars.Read` MUST be added to `GraphConnectorOptions.Scopes[]` via configuration.
The adapter MUST normalize all datetime values to UTC before returning results.

#### Scenario: Adapter fetches today's calendar events

- GIVEN the calendar adapter is invoked for today's date window
- WHEN `IConnectorAdapter.ExecuteAsync` is called
- THEN events from `GET /me/calendarView` with `startDateTime` and `endDateTime` are returned

#### Scenario: Adapter uses shared Graph client factory

- GIVEN the calendar adapter is registered in DI
- WHEN the adapter needs a `GraphServiceClient`
- THEN it calls `IGraphClientFactory.CreateClientAsync()` — the same factory used by Teams/Outlook

#### Scenario: Adapter gracefully handles Graph API failure

- GIVEN the Graph API returns an error (e.g., 403 Forbidden for missing scope)
- WHEN the adapter attempts to fetch calendar events
- THEN a failure result is returned with a non-empty reason string
- AND no exception propagates to the worker

---

### Requirement: Upcoming Meetings Use Case

The system MUST provide a `GetUpcomingMeetingsUseCase` that returns calendar events for
today plus a configurable lookahead window (default: 8 hours). The use case MUST read
from the calendar event store and return events sorted by start time ascending.

#### Scenario: Use case returns today's upcoming events

- GIVEN calendar events exist for today within the lookahead window
- WHEN `GetUpcomingMeetingsUseCase` is invoked
- THEN events are returned sorted by StartUtc ascending
- AND only events with StartUtc >= now and StartUtc <= now + window are included

#### Scenario: No upcoming events returns empty list

- GIVEN no calendar events exist within the lookahead window
- WHEN the use case is invoked
- THEN an empty list is returned with no error

---

### Requirement: Dashboard Calendar Panel

The system MUST provide an `UpcomingMeetingsPanel.razor` Blazor component that displays
upcoming meetings on the dashboard as a separate panel alongside `InboxPreviewPanel`.
The panel MUST render four states: loading, populated, empty, and error. The panel MUST
be presentation-only and MUST NOT contain business logic. Each event MUST display title,
start/end time, duration, and join URL (if online).

#### Scenario: Populated state shows meetings

- GIVEN upcoming meetings are returned by the use case
- WHEN the panel renders
- THEN each meeting shows title, start time, end time, duration, and join URL (if present)

#### Scenario: Empty state with no meetings

- GIVEN no upcoming meetings exist
- WHEN the panel renders
- THEN an explicit "No upcoming meetings" message is displayed

#### Scenario: Error state on calendar fetch failure

- GIVEN the calendar data fetch fails
- WHEN the panel renders
- THEN an explicit error message is displayed and the panel remains visible

---

### Requirement: Fixture Fallback

The calendar adapter SHOULD support a fixture fallback for controlled-demo mode. When a
pre-configured fixture delegate is provided, it MUST be used instead of the Graph API call.
This pattern MUST follow the same fixture approach used by Teams/Outlook adapters.

#### Scenario: Fixture mode bypasses Graph API

- GIVEN a fixture delegate is registered for the calendar adapter
- WHEN the adapter is invoked
- THEN the fixture delegate is called and its result is returned
- AND no Graph API call is made
