# Operational Dashboard — Delta Spec

## Purpose

Priority dashboard showing connector status cards (Outlook, Teams, Calendar) with real Graph data, sync controls, and ranked summary items. Replaces the scaffolded initial dashboard as the default view at `/`.

## ADDED Requirements

### Requirement: Stitch Design System Tokens

The UI MUST define CSS custom properties for the Stitch dark-mode design system. All colors, typography, and spacing MUST be tokenized and reusable across all dashboard screens.

#### Scenario: Design tokens defined in app.css

- GIVEN the application loads `app.css`
- WHEN CSS custom properties are inspected
- THEN the following tokens are defined: `--canvas` (#0F172A), `--card` (#1E293B), `--border` (#334155), `--primary` (#3b82f6), `--success` (#10b981), `--warning` (#f59e0b), `--offline` (#64748b)

#### Scenario: Typography tokens applied

- GIVEN the dashboard renders any text element
- WHEN font-family is inspected
- THEN UI text uses Inter and labels/metrics use JetBrains Mono
- AND font sizes follow the Stitch type scale

---

### Requirement: Connector Status Cards

The dashboard MUST display connector status cards for Outlook, Teams, and Calendar. Each card MUST show: connector name, status badge (emerald/amber/slate glow dot), item count, and last sync time.

#### Scenario: All connectors healthy

- GIVEN all three Graph connectors are enabled and have synced
- WHEN the dashboard renders
- THEN each connector card shows an emerald glow dot
- AND item counts and last sync times are populated from live data

#### Scenario: Connector disabled

- GIVEN `GraphConnector__Enabled=false`
- WHEN the dashboard renders
- THEN each connector card shows a slate glow dot
- AND the card displays "Disabled" status

#### Scenario: Connector partially configured

- GIVEN `GraphConnector__Enabled=true` but `TenantId` is missing
- WHEN the dashboard renders
- THEN each connector card shows an amber glow dot
- AND the card displays "Partial Config" status

---

### Requirement: Sync Button

The dashboard MUST provide a sync button that triggers a manual sync for all connectors. The button MUST show loading state during sync and update connector cards when sync completes.

#### Scenario: Manual sync triggered

- GIVEN the user clicks the sync button
- WHEN the sync request is sent to the API
- THEN the button shows a loading indicator
- AND connector cards show "Syncing" status

#### Scenario: Sync completes successfully

- GIVEN a manual sync is in progress
- WHEN the API returns success for all connectors
- THEN the sync button returns to idle state
- AND connector cards update with new item counts and last sync times

#### Scenario: Sync fails

- GIVEN a manual sync is in progress
- WHEN one or more connectors fail to sync
- THEN the sync button returns to idle state
- AND failed connector cards show error status with retry option

---

### Requirement: Ranked Summary Items

The dashboard MUST display a ranked list of summary items across all connectors. Items MUST be sorted by priority (importance/urgency) and show: item title, source connector, timestamp, and priority indicator.

#### Scenario: Items ranked by priority

- GIVEN multiple connectors have synced items
- WHEN the dashboard renders the summary list
- THEN items are sorted by priority score (highest first)
- AND each item shows source connector badge and timestamp

#### Scenario: Empty state for no items

- GIVEN all connectors are enabled but have zero items
- WHEN the dashboard renders
- THEN an empty state message is displayed (e.g., "No pending items across connectors")
- AND connector status cards remain visible

#### Scenario: Items from multiple connectors

- GIVEN Outlook has 5 items and Teams has 3 items
- WHEN the dashboard renders the summary list
- THEN all 8 items appear in the ranked list
- AND items from different connectors are visually distinguishable

---

### Requirement: Loading, Empty, and Error States

The dashboard MUST implement all four view states: loading, empty, error, and populated. The loading state MUST use a skeleton or spinner consistent with the Stitch design. The error state MUST include the error message and a retry affordance.

#### Scenario: Loading state shown during data fetch

- GIVEN the dashboard initiates an API call
- WHEN the response has not yet arrived
- THEN a loading skeleton or spinner is displayed
- AND the dashboard shell remains interactive

#### Scenario: Populated state replaces loading

- GIVEN the API returns valid data
- WHEN the dashboard processes the response
- THEN the loading state is replaced with populated content
- AND data is rendered with correct typography and token styling
