# Initial Dashboard Specification

## Purpose

The initial dashboard provides Aura's first demonstrable UI slice through a separate Blazor Server app that renders the Stitch-derived shell and consumes `Aura.Api` over HTTP only.

## Requirements

### Requirement: Separate Dashboard Host

The system MUST provide an `Aura.UI` host as a separate solution project and SHALL render the initial dashboard shell at its default entry route.

#### Scenario: Dashboard shell renders
- GIVEN `Aura.UI` is started with its required configuration
- WHEN a user opens the default dashboard route
- THEN the UI SHALL render the sidebar, header, and main dashboard container
- AND the shell SHALL be visible before optional dashboard data is populated

#### Scenario: Shell survives missing dashboard data
- GIVEN the shell route is reachable
- AND dashboard content data is not yet available
- WHEN the page is rendered
- THEN the user SHALL still see the layout shell and a non-crashing placeholder state

### Requirement: API-Only UI Integration

The UI MUST consume dashboard data from `Aura.Api` HTTP endpoints only. It MUST NOT bypass `Aura.Api` by directly depending on `Aura.Application`, `Aura.Domain`, or `Aura.Infrastructure` runtime services.

#### Scenario: Data is loaded through API contracts
- GIVEN the dashboard requires visible summary data
- WHEN the UI requests that data
- THEN the request SHALL target an `Aura.Api` HTTP endpoint
- AND the rendered values SHALL come from API DTO contracts

#### Scenario: API failure does not bypass boundaries
- GIVEN the dashboard HTTP request fails or returns an unavailable result
- WHEN the page handles the failure
- THEN the UI SHALL show an error state
- AND it SHALL NOT switch to direct in-process service access as a fallback

### Requirement: Visible View States

The dashboard MUST expose loading, empty, error, and populated states for its initial visible content so the slice remains demonstrable and testable.

#### Scenario: Loading transitions to populated state
- GIVEN the dashboard starts a data request
- WHEN the API responds with dashboard data
- THEN the UI SHALL first show a loading state
- AND then replace it with populated content

#### Scenario: Empty result is explicit
- GIVEN the API returns a successful response with no dashboard items for the initial slice
- WHEN the UI renders the response
- THEN the UI SHALL show an explicit empty state
- AND it SHALL keep the shell navigation available

### Requirement: Repository-Realistic Smoke Verification

The change MUST include automated smoke verification that fits the repository's current .NET test/build reality. It SHALL verify the initial dashboard scaffold without requiring Playwright until Playwright is actually configured.

#### Scenario: Smoke verification proves the slice is wired
- GIVEN the solution test/build workflow supported by the repository
- WHEN the dashboard change is verified
- THEN automated checks SHALL prove `Aura.UI` is part of the solution and the dashboard scaffold can be exercised

#### Scenario: Unsupported browser tooling is not assumed
- GIVEN Playwright tooling is not configured in the repository
- WHEN verification requirements are evaluated
- THEN the capability SHALL remain compliant through non-Playwright automated smoke coverage
- AND it SHALL NOT claim mandatory Playwright coverage for this slice

---

### Requirement: Layout Shell Responsive Adaptation

The dashboard shell (header + sidebar + main content) MUST adapt at 900/768/480px breakpoints. At ≤900px the sidebar becomes a hamburger drawer overlay. At ≤768px the header compresses non-critical controls into an overflow area. The Sign out button and Settings icon MUST remain visible at all breakpoints.

(Previously: Shell had a single 900px breakpoint that collapsed sidebar to full-width inline and hid header nav.)

#### Scenario: Header at ≤768px shows hamburger + essential controls

- GIVEN viewport ≤768px
- WHEN the header renders
- THEN the hamburger icon is visible on the left
- AND Sign out + Settings remain visible on the right
- AND Demo Mode / FocusState badge are accessible via overflow or remain visible if space permits

#### Scenario: Status greeting card badges wrap at ≤480px

- GIVEN viewport ≤480px and StatusGreetingCard has 5 status badges
- WHEN the card renders
- THEN badges wrap to a second row without horizontal overflow
