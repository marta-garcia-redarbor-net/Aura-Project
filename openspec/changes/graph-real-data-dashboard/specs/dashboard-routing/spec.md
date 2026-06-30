# Dashboard Routing — Delta Spec

## Purpose

Separate the existing dashboard (mock/fixture data) from the new priority dashboard (live Graph data) by assigning distinct routes. The new priority dashboard becomes the default view at `/`, and the old dashboard is accessible at `/test-dashboard`.

## ADDED Requirements

### Requirement: Route Separation

The UI MUST serve the new priority dashboard at `/` (default route) and the old dashboard at `/test-dashboard`. Both dashboards MUST be independently accessible without affecting each other's state or data.

#### Scenario: Default route shows priority dashboard

- GIVEN the application is started
- WHEN a user navigates to `/`
- THEN the priority dashboard renders with Stitch dark-mode design
- AND connector status cards are displayed

#### Scenario: Old dashboard accessible at /test-dashboard

- GIVEN the application is started
- WHEN a user navigates to `/test-dashboard`
- THEN the existing dashboard renders with its current mock/fixture data
- AND all existing panels remain functional

#### Scenario: Route coexistence does not break navigation

- GIVEN the user is on the priority dashboard at `/`
- WHEN the user navigates to `/test-dashboard`
- THEN the old dashboard renders without error
- AND navigating back to `/` returns to the priority dashboard

---

### Requirement: Old Dashboard Preservation

The existing dashboard MUST remain fully functional at `/test-dashboard`. No existing panels, endpoints, or behavior MUST be modified or removed as part of this routing change. The old dashboard MUST continue to consume the same API endpoints it currently uses.

#### Scenario: Old panels unchanged

- GIVEN the user navigates to `/test-dashboard`
- WHEN any existing panel is inspected
- THEN all panels render with their current data sources and behavior
- AND no new Stitch design tokens are applied to the old view

#### Scenario: Old API endpoints unaffected

- GIVEN the old dashboard is at `/test-dashboard`
- WHEN `GET /api/dashboard/preview` or `GET /api/dashboard/initial` is called
- THEN the response is identical to the pre-change behavior
- AND no new endpoints are required for the old dashboard

---

### Requirement: Default Route Redirect

If a user navigates to an unknown route (not `/`, `/test-dashboard`, or any other defined route), the system SHOULD redirect to `/` (the priority dashboard) rather than showing a 404 page.

#### Scenario: Unknown route redirects to priority dashboard

- GIVEN the user navigates to `/nonexistent-route`
- WHEN the router processes the route
- THEN the user is redirected to `/`
- AND the priority dashboard renders

#### Scenario: Known routes are not redirected

- GIVEN the user navigates to `/test-dashboard`
- WHEN the router processes the route
- THEN no redirect occurs
- AND the old dashboard renders directly
