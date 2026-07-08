# Sync UI — Delta Spec

## Purpose

Wire the SyncStatusPanel sync-now button to the backend `POST /api/sync/now` endpoint, displaying per-source sync status, item counts, and error states in the UI.

## MODIFIED Requirements

### Requirement: Connector Execution Result

The sync-now button MUST trigger `POST /api/sync/now` and display per-source results (Outlook, Teams, Calendar) including item count, last sync time, and error state for each source. The button MUST show loading state while the sync request is in flight.

#### Scenario: Sync button triggers API call

- GIVEN the SyncStatusPanel is rendered
- WHEN the user clicks the "Sync Now" button
- THEN a POST request is sent to `/api/sync/now`
- AND the button enters a loading/disabled state during the request

#### Scenario: Successful sync displays per-source results

- GIVEN a POST to `/api/sync/now` returns HTTP 200 with results for 3 sources
- WHEN the SyncStatusPanel processes the response
- THEN each source (Outlook, Teams, Calendar) displays its item count and last sync timestamp
- AND the sync button returns to enabled state

#### Scenario: Partial sync failure shows mixed status

- GIVEN a POST to `/api/sync/now` returns HTTP 200 with 2 successes and 1 failure
- WHEN the SyncStatusPanel processes the response
- THEN the two successful sources show green/healthy status with item counts
- AND the failed source shows error status with the failure reason
- AND the sync button returns to enabled state

#### Scenario: Network failure shows error state

- GIVEN a POST to `/api/sync/now` fails with a network error
- WHEN the SyncStatusPanel handles the failure
- THEN an error message is displayed
- AND the sync button returns to enabled state
- AND existing panel data is preserved (not cleared)

---

## ADDED Requirements

### Requirement: Per-Source Sync Status Display

The SyncStatusPanel MUST display per-source sync information including source name, status badge (emerald=healthy, amber=warning, slate=offline), item count, and last sync timestamp. Each source MUST have a distinct visual representation matching the Stitch design system.

#### Scenario: All sources healthy

- GIVEN all three connectors have successfully synced
- WHEN the SyncStatusPanel renders
- THEN each source shows an emerald status badge
- AND item counts are displayed for each source
- AND last sync timestamps are shown in relative format (e.g., "2 min ago")

#### Scenario: Source never synced

- GIVEN a connector has never been synced (no previous execution)
- WHEN the SyncStatusPanel renders
- THEN the source shows a slate status badge
- AND the item count shows "--" or equivalent empty state
- AND the last sync time shows "Never"

#### Scenario: API endpoint unavailable

- GIVEN the `/api/sync/status` endpoint returns HTTP 5xx
- WHEN the SyncStatusPanel handles the error
- THEN each source shows an error state with a retry affordance
- AND the dashboard shell and navigation remain functional
