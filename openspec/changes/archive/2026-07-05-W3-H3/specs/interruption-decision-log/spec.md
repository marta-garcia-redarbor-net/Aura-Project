# Interruption Decision Log Specification

## Purpose

Define the sidebar navigation entry, page layout, and data display for the interruption decision history, letting users audit every triage verdict (INTERRUPT, QUEUE, DEFER) with full context.

## Requirements

### Requirement: Sidebar Navigation Entry

The application sidebar MUST include an "Interruption Log" menu entry under the triage or tools section. The entry MUST navigate to `/triage/decisions`. The entry SHALL display a clock or list icon.

#### Scenario: Menu entry navigates to log page

- GIVEN the sidebar is rendered
- WHEN the user clicks "Interruption Log"
- THEN the browser navigates to `/triage/decisions`

#### Scenario: Active state highlights correctly

- GIVEN the user is on the Interruption Log page
- WHEN the sidebar renders
- THEN the "Interruption Log" entry is highlighted as active

### Requirement: Decision History Page

The page at `/triage/decisions` MUST fetch and display paginated decision history from `GET /api/triage/decisions`. The page MUST support the four UI states: loading, populated, empty, and error.

#### Scenario: Loading state

- GIVEN the page has mounted and the API call is in flight
- WHEN the page renders
- THEN a loading indicator is visible and no stale data is shown

#### Scenario: Populated state with table

- GIVEN the API returns a non-empty decision list
- WHEN the page renders
- THEN a table lists each decision with all fields visible

#### Scenario: Empty state

- GIVEN the API returns zero decisions
- WHEN the page renders
- THEN an explicit "No decisions recorded yet" message is shown

#### Scenario: Error state

- GIVEN the API call fails
- WHEN the page renders
- THEN an explicit error message is displayed
- AND a retry button is available

### Requirement: Decision History Table Columns

The decision history table MUST display these columns: Timestamp, Title, Source, Priority Score, Decision, Focus State, Explanation. The table SHALL be sorted by Timestamp DESC by default. Each row SHALL be clickable to navigate to the work item detail view.

#### Scenario: All columns rendered correctly

- GIVEN a decision with title "Urgent PR review", source "pr-review", score 88, decision "INTERRUPT", focus "WindowOfOpportunity"
- WHEN the table renders
- THEN every column is populated with the correct value

#### Scenario: Click navigates to detail

- GIVEN the user clicks a decision row
- WHEN the click is registered
- THEN the browser navigates to `/workitems/{workItemId}`

### Requirement: Pagination Controls

The decision history page MUST include pagination controls when the result set exceeds one page. The page size SHALL default to 20. Page navigation SHALL refetch via `?page=N`.

#### Scenario: Pagination appears when needed

- GIVEN 50 decisions exist with a page size of 20
- WHEN the page loads
- THEN 20 items are displayed
- AND page 2 and 3 navigation links are visible

#### Scenario: Single page hides pagination

- GIVEN fewer than 20 decisions exist
- WHEN the page loads
- THEN no pagination controls are shown
