# Health Page Specification

## Purpose

The health page provides a dedicated route at `/health` displaying system status, module progress, and Graph connector health panels — extracted from the main dashboard.

## Requirements

### Requirement: Health Page Route

The system MUST expose a `/health` route that renders `SystemStatusPanel`, `ModuleProgressPanel`, and `GraphConnectorStatusPanel` in an `AuthorizeView` wrapper.

#### Scenario: Authorized user views health page

- GIVEN an authenticated user navigates to `/health`
- WHEN the page loads
- THEN `SystemStatusPanel`, `ModuleProgressPanel`, and `GraphConnectorStatusPanel` SHALL be rendered
- AND the sidebar SHALL highlight the Health nav item

#### Scenario: Unauthorized access is blocked

- GIVEN an unauthenticated user navigates to `/health`
- WHEN the page renders
- THEN the system SHALL display the authentication challenge or redirect to login
- AND no health panel data SHALL be visible

### Requirement: Sidebar Navigation Link

The sidebar Health nav item MUST be an anchor link pointing to `/health` instead of a non-navigating `<span>`.

#### Scenario: Sidebar Health link navigates correctly

- GIVEN the sidebar is rendered
- WHEN a user clicks the Health nav item
- THEN the browser SHALL navigate to `/health`
- AND the health page SHALL load with all three panels

#### Scenario: Sidebar link is an anchor element

- GIVEN the sidebar renders the Health nav item
- WHEN the DOM is inspected
- THEN the Health nav item SHALL be an `<a>` element with `href="/health"`

### Requirement: Dashboard No Longer Shows Health Panels

The dashboard at `/` MUST NOT render `GraphConnectorStatusPanel`, `SystemStatusPanel`, or `ModuleProgressPanel`. These panels are exclusive to `/health`.

#### Scenario: Dashboard omits health panels

- GIVEN a user navigates to `/`
- WHEN the dashboard renders
- THEN `RankedSummaryList` and connector cards SHALL be present
- AND `SystemStatusPanel`, `ModuleProgressPanel`, and `GraphConnectorStatusPanel` SHALL NOT be rendered
