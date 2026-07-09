# Delta for Dashboard Routing

## MODIFIED Requirements

### Requirement: Route Separation

The UI MUST serve the new priority dashboard at `/dashboard` and the old dashboard at `/test-dashboard`. The root route `/` MUST serve the public landing page. Both dashboards MUST be independently accessible without affecting each other's state or data.
(Previously: Priority dashboard at `/`, old dashboard at `/test-dashboard`.)

#### Scenario: Dashboard route shows priority dashboard

- GIVEN the application is started
- WHEN a user navigates to `/dashboard`
- THEN the priority dashboard renders with Stitch dark-mode design
- AND connector status cards are displayed

#### Scenario: Old dashboard accessible at /test-dashboard

- GIVEN the application is started
- WHEN a user navigates to `/test-dashboard`
- THEN the existing dashboard renders with its current mock/fixture data
- AND all existing panels remain functional

#### Scenario: Route coexistence does not break navigation

- GIVEN the user is on the priority dashboard at `/dashboard`
- WHEN the user navigates to `/test-dashboard`
- THEN the old dashboard renders without error
- AND navigating back to `/dashboard` returns to the priority dashboard

---

### Requirement: Default Route Redirect

If a user navigates to an unknown route (not `/`, `/dashboard`, `/test-dashboard`, or any other defined route), the system SHOULD redirect to `/dashboard` for authenticated users or `/` for unauthenticated users.
(Previously: Unknown routes always redirected to `/`.)

#### Scenario: Unknown route redirects authenticated user to dashboard

- GIVEN the user is authenticated
- WHEN the user navigates to `/nonexistent-route`
- THEN the user is redirected to `/dashboard`
- AND the priority dashboard renders

#### Scenario: Unknown route redirects unauthenticated user to landing

- GIVEN the user is not authenticated
- WHEN the user navigates to `/nonexistent-route`
- THEN the user is redirected to `/`
- AND the landing page renders

#### Scenario: Known routes are not redirected

- GIVEN the user navigates to `/test-dashboard`
- WHEN the router processes the route
- THEN no redirect occurs
- AND the old dashboard renders directly
