# Dashboard Inbox Preview Specification

## Purpose

Defines the API endpoint, slim DTO projections, and Blazor panel behavior for the dashboard
inbox-by-source view and Morning Summary preview card. Domain `WorkItem` never crosses the
UI/API boundary.

## Requirements

### Requirement: Preview Endpoint Contract

`GET /dashboard/preview` MUST return inbox-by-source groups filtered to `Status = Pending` items only. Each inbox item MUST include: title/subject, source, relative timestamp, relevance score, unread count, and brief suggested action. `WorkItem` and domain aggregates MUST NOT appear in any response type.
(Previously: no status filter; no unread count in inbox items)

#### Scenario: Only Pending items returned

- GIVEN completed and pending WorkItems exist across sources
- WHEN `GET /dashboard/preview` is called
- THEN the response contains only items with Pending status
- AND no Completed or Faulted items are present

#### Scenario: Inbox items include UnreadCount

- GIVEN multiple pending inbox items with varying unread counts
- WHEN `GET /dashboard/preview` is called
- THEN each inbox item DTO carries an `UnreadCount` property matching the source metadata

#### Scenario: All items completed returns empty inbox

- GIVEN all WorkItems have been read and marked Completed
- WHEN `GET /dashboard/preview` is called
- THEN the inbox groups are empty
- AND the response is HTTP 200

### Requirement: Dashboard Panel UI States

`InboxPreviewPanel` and `MorningSummaryPreviewPanel` MUST each render four distinct states:
loading, empty, error, and populated. Panels MUST be presentation-only; they MUST NOT contain
business logic. On error, the panel MUST display an explicit error message and MUST NOT silently
hide the panel block.

#### Scenario: Loading state

- GIVEN the panel has mounted and the API call is in flight
- WHEN the component renders
- THEN a loading indicator is visible and no stale data is shown

#### Scenario: Populated state

- GIVEN the API returns a non-empty preview payload
- WHEN the component renders
- THEN items are displayed with all required fields visible

#### Scenario: Empty state

- GIVEN the API returns an empty collection
- WHEN the component renders
- THEN an explicit empty-state message is shown and no items are rendered

#### Scenario: Error state

- GIVEN the API call fails or returns a non-success status
- WHEN the component renders
- THEN an explicit error message is displayed and the panel block remains visible

### Requirement: DTO Boundary Enforcement

API response types and UI model classes MUST use dashboard-specific slim DTOs exclusively.
`WorkItem` MUST NOT appear in API response types, UI model classes, or Blazor component
parameters.

#### Scenario: Architecture boundary check

- GIVEN the implemented endpoint and UI models
- WHEN an architecture test inspects API response types and the UI Models namespace
- THEN no reference to `WorkItem` or domain aggregate types is found in those types

### Requirement: Smoke Verification via WebApplicationFactory

The preview endpoint MUST be covered by a smoke test using `WebApplicationFactory`.
Playwright MUST NOT be used for this slice.

#### Scenario: Endpoint responds successfully

- GIVEN a running test host via `WebApplicationFactory`
- WHEN `GET /dashboard/preview` is called
- THEN the response returns HTTP 200 with a deserializable slim DTO payload
- AND no domain types appear in the deserialized shape

### Requirement: Calendar Panel on Dashboard

The dashboard MUST display an `UpcomingMeetingsPanel` as a separate panel alongside the
existing `InboxPreviewPanel`. The calendar panel is independent — it reads from the
calendar domain, not from the WorkItem inbox. The panel MUST render four states:
loading, populated, empty, and error, following the same presentation-only pattern
as existing dashboard panels.

#### Scenario: Calendar panel renders alongside inbox panel

- GIVEN the dashboard is loaded
- WHEN both inbox and calendar data are available
- THEN `InboxPreviewPanel` and `UpcomingMeetingsPanel` render as separate panels on the dashboard

#### Scenario: Calendar panel loading state

- GIVEN the dashboard is loaded and calendar data is fetching
- WHEN `UpcomingMeetingsPanel` renders
- THEN a loading indicator is visible and no stale data is shown

#### Scenario: Calendar panel populated state

- GIVEN upcoming meetings are available
- WHEN `UpcomingMeetingsPanel` renders
- THEN meetings are displayed with title, time, duration, and join URL

#### Scenario: Calendar panel empty state

- GIVEN no upcoming meetings exist
- WHEN `UpcomingMeetingsPanel` renders
- THEN an explicit empty-state message is shown

#### Scenario: Calendar panel error state

- GIVEN the calendar data fetch fails
- WHEN `UpcomingMeetingsPanel` renders
- THEN an explicit error message is displayed and the panel remains visible

---

### Requirement: UnreadCount on InboxItemPreviewDto

`InboxItemPreviewDto` MUST expose an `UnreadCount` property as `{ get; init; }`. This property SHALL NOT be part of the positional constructor — it is added via init-only setter to avoid breaking the existing positional record contract.

#### Scenario: UnreadCount populated from Metadata

- GIVEN a WorkItem with `Metadata["unreadCount"] = "5"`
- WHEN the DTO projection runs
- THEN `InboxItemPreviewDto.UnreadCount` equals `5`

#### Scenario: UnreadCount absent defaults to zero

- GIVEN a WorkItem without `unreadCount` in Metadata
- WHEN the DTO projection runs
- THEN `InboxItemPreviewDto.UnreadCount` equals `0`
