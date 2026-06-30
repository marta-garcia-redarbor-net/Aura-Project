# Delta for Dashboard Reorder

## MODIFIED Requirements

### Requirement: Visible View States

The dashboard MUST expose loading, empty, error, and populated states for its initial visible content so the slice remains demonstrable and testable. The `RankedSummaryList` component MUST render before the connector cards grid in `PriorityDashboard`.

#### Scenario: Default rendering order

- GIVEN the dashboard is rendered at `/`
- WHEN the page loads successfully
- THEN `RankedSummaryList` SHALL appear above the connector cards grid
- AND `SyncButton` SHALL remain adjacent to the connector cards

#### Scenario: Loading state preserves order

- GIVEN the dashboard starts a data request
- WHEN the API responds with dashboard data
- THEN the loading placeholder for `RankedSummaryList` SHALL appear before the connector cards grid
- AND the populated content SHALL replace loading in the same position

#### Scenario: Error state preserves order

- GIVEN the dashboard HTTP request fails
- WHEN the UI renders the error state
- THEN `RankedSummaryList` SHALL remain before the connector cards grid
- AND the shell navigation SHALL stay available

## REMOVED Requirements

### Requirement: Dashboard Health Panels

(Reason: Health/status panels are moved to a dedicated `/health` page. The dashboard no longer renders `GraphConnectorStatusPanel`, `SystemStatusPanel`, or `ModuleProgressPanel`.)
(Migration: Users navigate to `/health` for system health information.)
