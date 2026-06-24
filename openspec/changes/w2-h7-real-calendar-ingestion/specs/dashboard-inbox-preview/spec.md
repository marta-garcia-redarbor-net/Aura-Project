# Delta for Dashboard Inbox Preview

## ADDED Requirements

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
