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

---

### Requirement: PriorityScore on Dashboard DTOs

`InboxItemPreviewDto` and all dashboard-scope DTOs MUST carry a nullable `PriorityScore` property. Items SHALL be rendered in descending PriorityScore order in dashboard views. Items with equal score SHALL be sub-ordered by `capturedAtUtc` DESC.

#### Scenario: DTO carries PriorityScore

- GIVEN a pending `WorkItem` with `PriorityScore = 85`
- WHEN `GET /dashboard/preview` is called
- THEN the corresponding DTO has `priorityScore: 85`

#### Scenario: Dashboard items sorted by priority

- GIVEN pending items with PriorityScore 90, 60, and null (Normal → 50)
- WHEN the dashboard inbox panel renders
- THEN items display in order: 90, 60, 50

---

### Requirement: Priority Counts and Banding

The dashboard MUST display two summary stats: total pending count and high-priority count. The system SHALL define high-priority as `PriorityScore >= 75` or the Critical-priority derived default. Groups with non-zero high-priority count SHALL display a visual badge.

#### Scenario: Dashboard shows pending and high-priority counts

- GIVEN 15 pending items, 4 of which have PriorityScore >= 75
- WHEN the dashboard renders
- THEN "15 pending" and "4 high priority" are visible as summary stats

#### Scenario: Zero high-priority renders zero

- GIVEN no items have PriorityScore >= 75
- WHEN the dashboard renders
- THEN the high-priority count displays "0"

---

### Requirement: Top-3 Priority Highlighting

The dashboard inbox panel MUST highlight the top 3 highest-priority items. Each highlighted item MUST display a visible importance badge. Items with equal PriorityScore SHALL be sub-ordered by `capturedAtUtc` DESC to determine the top-3 boundary.

#### Scenario: Top-3 highlighted with badge

- GIVEN 10 pending items, the top 3 have scores 95, 90, 85
- WHEN the dashboard inbox panel renders
- THEN the first 3 items display an importance badge
- AND items 4+ do not display the badge

#### Scenario: Fewer than 3 items all highlighted

- GIVEN only 2 pending items exist
- WHEN the dashboard inbox panel renders
- THEN both items display the importance badge

#### Scenario: Tie at boundary includes all tied items

- GIVEN items with scores: 95, 90, 85, 85 (fourth tied with third)
- WHEN the dashboard inbox panel renders
- THEN all four items with score 85+ are highlighted
- AND items below 85 are not highlighted

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
