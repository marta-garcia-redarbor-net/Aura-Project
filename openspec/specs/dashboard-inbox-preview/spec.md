# Dashboard Inbox Preview Specification

## Purpose

Defines the API endpoint, slim DTO projections, and Blazor panel behavior for the dashboard
inbox-by-source view and Morning Summary preview card. Domain `WorkItem` never crosses the
UI/API boundary.

## Requirements

### Requirement: Preview Endpoint Contract

`GET /dashboard/preview` MUST return inbox-by-source groups and a Morning Summary preview as
slim DTOs. Each inbox item MUST include: title/subject, source, relative timestamp, relevance
score, and brief suggested action. The response MUST also include real data fields: deep link, priority hint, snippet, and sync state. The Morning Summary preview MUST contain summarized ranking
only — no drill-down, expanded detail, or scoring internals. `WorkItem` and domain aggregates
MUST NOT appear in any response type.

#### Scenario: Inbox items populated

- GIVEN ranked inbox items exist across multiple sources
- WHEN `GET /dashboard/preview` is called
- THEN the response contains source-keyed groups, each with items carrying title, source, relative timestamp, score, suggested action, deep link, priority hint, snippet, and sync state

#### Scenario: Morning summary populated

- GIVEN morning-summary-ranking output is available
- WHEN `GET /dashboard/preview` is called
- THEN the response contains a non-empty summarized ranking list
- AND no raw source data, domain type, or expanded detail is present

#### Scenario: Both sections empty

- GIVEN no ranked items and no summary output are available
- WHEN `GET /dashboard/preview` is called
- THEN the response returns HTTP 200 with empty inbox groups and empty summary list

### Requirement: Manual Sync Trigger and Feedback

The UI MUST provide a manual "Sync now" trigger. This control MUST display granular loading states, per-source progress, result counts, and the last sync timestamp.

#### Scenario: User triggers manual sync

- GIVEN the dashboard is loaded and a sync is not currently in progress
- WHEN the user clicks "Sync now"
- THEN the UI shows a loading state
- AND the UI displays per-source progress and result counts upon completion
- AND the last sync timestamp is updated

---

### Requirement: Dashboard Panel UI States

`InboxPreviewPanel` and `MorningSummaryPreviewPanel` MUST each render four distinct states:
loading, empty, error, and populated. Panels MUST be presentation-only; they MUST NOT contain
business logic. On error, the panel MUST display an explicit error message and MUST NOT silently
hide the panel block. Empty data states MUST be explicit in the UI and MUST NOT silently fall back to demo data.

#### Scenario: Loading state

- GIVEN the panel has mounted and the API call is in flight
- WHEN the component renders
- THEN a loading indicator is visible and no stale data is shown

#### Scenario: Populated state

- GIVEN the API returns a non-empty preview payload
- WHEN the component renders
- THEN items are displayed with all required fields visible including deep links and snippets

#### Scenario: Explicit empty state with no demo fallback

- GIVEN the API returns an empty collection for a successfully synced source
- WHEN the component renders
- THEN an explicit empty-state message is shown
- AND no demo data is displayed

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
