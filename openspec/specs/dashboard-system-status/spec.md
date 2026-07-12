# Dashboard System Status Specification

## Purpose

Derive and expose tri-state (OK / Warning / Error) readiness indicators for API,
Qdrant, and mock-auth, server-side via Application/Infrastructure ports. The UI
renders these indicators read-only from `Aura.Api` DTO contracts. Auth indicator
reflects provider configuration only — never current user session state.

## Requirements

### Requirement: Status Derivation

The system MUST derive a tri-state status (`OK`, `Warning`, `Error`) for each
indicator (API, Qdrant, mock-auth) in the Application layer through port interfaces
implemented in Infrastructure. Each derived state MUST carry brief explanatory
microcopy. Derivation logic MUST NOT reside in the UI or Api layer.

#### Scenario: All indicators healthy

- GIVEN all readiness adapters report healthy signals
- WHEN system status derivation runs
- THEN each indicator returns `OK` with its corresponding microcopy

#### Scenario: One indicator degraded

- GIVEN one readiness adapter reports a degraded signal
- WHEN system status derivation runs
- THEN that indicator returns `Warning` with explanatory microcopy
- AND remaining indicators return their independently derived states

#### Scenario: One indicator unavailable

- GIVEN one readiness adapter reports an unavailable signal
- WHEN system status derivation runs
- THEN that indicator returns `Error` with explanatory microcopy

---

### Requirement: Mock-Auth Indicator Scope

The mock-auth status indicator MUST reflect whether the mock-auth provider is
configured and active. It MUST NOT reflect current user session state or any
authentication outcome.

#### Scenario: Provider configured and active

- GIVEN the mock-auth provider is registered and active in the application bootstrap
- WHEN the mock-auth indicator is derived
- THEN the indicator state is `OK`

#### Scenario: Provider not configured

- GIVEN the mock-auth provider is absent or inactive
- WHEN the mock-auth indicator is derived
- THEN the indicator state is `Warning` or `Error` with explanatory microcopy

#### Scenario: Session state does not influence indicator

- GIVEN no user is currently authenticated
- WHEN the mock-auth indicator is derived
- THEN the state is determined solely by provider configuration, not session state

---

### Requirement: Status API Endpoint

The API MUST expose a GET-only endpoint returning a DTO with all 5 indicator
states (api, db, qdrant, llm, mockAuth). Write verbs (POST, PUT, PATCH, DELETE)
MUST NOT be accepted on this endpoint.

#### Scenario: GET returns all fields

- GIVEN any valid runtime configuration
- WHEN a client sends GET to the system-status endpoint
- THEN the response is HTTP 200 with a DTO containing `api`, `db`, `qdrant`, `llm`,
  and `mockAuth` fields, each carrying a `state` and `microcopy` value

#### Scenario: Write verbs rejected

- GIVEN the system-status endpoint is available
- WHEN a client sends POST, PUT, PATCH, or DELETE
- THEN the response is HTTP 405 Method Not Allowed

### Requirement: Recent Errors Endpoint

The API MUST expose `GET /api/dashboard/recent-errors` returning the last N errors. Each error MUST include a correlation ID, timestamp, and message. The endpoint MUST be read-only and scoped to the same authorization group as the system-status endpoint.

#### Scenario: Recent errors returned with correlation ID

- GIVEN errors have been logged with correlation IDs
- WHEN a client sends GET to the recent-errors endpoint
- THEN the response is HTTP 200 with a list of errors, each containing correlation ID, timestamp, and message

#### Scenario: No errors returns empty list

- GIVEN no errors have been logged recently
- WHEN a client sends GET to the recent-errors endpoint
- THEN the response is HTTP 200 with an empty array

#### Scenario: Write verbs rejected

- GIVEN the recent-errors endpoint is available
- WHEN a client sends POST, PUT, PATCH, or DELETE
- THEN the response is HTTP 405 Method Not Allowed

### Requirement: Error Correlation in UI

The dashboard system-status panel MUST display the most recent errors alongside the existing readiness indicators. Each displayed error MUST show its correlation ID as a clickable or copyable reference for log lookup.

#### Scenario: Panel renders errors with correlation IDs

- GIVEN the recent-errors endpoint returns 3 errors
- WHEN the status panel loads
- THEN each error is rendered with its correlation ID, timestamp, and message
- AND the existing readiness indicators remain unchanged

---

### Requirement: Read-Only Status Panel

The UI MUST render each indicator with its state and microcopy in a read-only panel.
All three states (OK, Warning, Error) MUST have a distinct visual representation.
The panel MUST NOT expose any edit or submit affordance. Loading and error view states
MUST be present alongside the populated state.

#### Scenario: Indicators render from DTO

- GIVEN the API returns all three indicators with state and microcopy
- WHEN the status panel loads
- THEN each indicator is displayed with its state label and microcopy text
- AND no edit controls are present

#### Scenario: API failure shows error state

- GIVEN the system-status API call fails or returns a non-200 response
- WHEN the panel handles the failure
- THEN the panel shows an explicit error state
- AND the dashboard shell and navigation remain functional

---

### Requirement: Database and LLM Readiness Indicators

The system MUST derive tri-state status for Database (via IDbReadinessProvider → DbReadinessAdapter → HealthCheckService) and LLM (via ILlmReadinessProvider → LlmReadinessAdapter → HTTP GET `/api/tags`, ≤3s timeout). Both follow the existing OK/Warning/Error pattern.

#### Scenario: Database healthy

- GIVEN DbHealthCheck reports healthy
- WHEN the Database indicator is derived
- THEN it is OK with corresponding microcopy

#### Scenario: Database unavailable

- GIVEN DbHealthCheck reports unhealthy
- WHEN the Database indicator is derived
- THEN it is Error with explanatory microcopy

#### Scenario: LLM healthy

- GIVEN Ollama `/api/tags` responds within 3s with HTTP 200
- WHEN the LLM indicator is derived
- THEN it is OK with corresponding microcopy

#### Scenario: LLM unavailable or times out

- GIVEN Ollama `/api/tags` is unreachable or times out after 3s
- WHEN the LLM indicator is derived
- THEN it is Error with explanatory microcopy

---

### Requirement: Overall Status Aggregation

The card MUST compute an Overall status client-side from the 5 individual indicators: OK only when ALL five (API, Database, Qdrant, LLM, mock-auth) are OK; Warning if any is Warning and none is Error; Error if any is Error. This is a UI concern — no DTO field needed.

#### Scenario: All sub-indicators healthy

- GIVEN all five sub-indicators report OK
- WHEN Overall is computed
- THEN Overall is OK

#### Scenario: One sub-indicator fails

- GIVEN Database reports Error and all others are OK
- WHEN Overall is computed
- THEN Overall is Error

#### Scenario: One sub-indicator degraded

- GIVEN one sub-indicator reports Warning and the rest are OK or Warning
- WHEN Overall is computed
- THEN Overall is Warning

---

### Requirement: Greeting Computation

The system MUST compute a time-of-day greeting from server local time: "Good morning" before 12:00, "Good afternoon" 12:00–17:59, "Good evening" 18:00+. The greeting MUST include the UserDisplayName from the CascadingValue.

#### Scenario: Morning greeting

- GIVEN server local time is 09:30
- WHEN the greeting is computed
- THEN it reads "Good morning, {UserDisplayName}"

#### Scenario: Afternoon greeting at noon boundary

- GIVEN server local time is 12:00
- WHEN the greeting is computed
- THEN it reads "Good afternoon, {UserDisplayName}"

#### Scenario: Evening greeting at 18:00 boundary

- GIVEN server local time is 18:00
- WHEN the greeting is computed
- THEN it reads "Good evening, {UserDisplayName}"

---

### Requirement: Status Greeting Card

The dashboard MUST render a compact StatusGreetingCard at the top of `/dashboard` showing the greeting and five status dots (Overall, API, Database, Qdrant, LLM). Dots MUST be green for OK, red for Error, grey for Degraded. The card MUST be ≤60px height and MUST NOT push priority cards below the fold. Each dot MUST have an aria-label describing its name and state.

#### Scenario: All systems healthy

- GIVEN all five indicators are OK and greeting is computed
- WHEN the card renders
- THEN greeting is shown, all five dots are green, card height ≤60px

#### Scenario: LLM unavailable shows red dot

- GIVEN LLM is Error, API is OK, DB is OK, Qdrant is OK, mock-auth is OK
- WHEN the card renders
- THEN the LLM dot is red, the Overall dot is red, the remaining dots are green

#### Scenario: API unresponsive renders gracefully

- GIVEN SystemStatusApiClient.GetStatusAsync throws
- WHEN the card renders
- THEN "System status unavailable" is shown and the dashboard remains functional

#### Scenario: Dots have accessible labels

- GIVEN the card displays five status dots
- THEN each dot has an aria-label matching the format "{indicator name}: {state}"

---

### Requirement: Data Freshness

The card MUST re-fetch status when EventBus.OnDashboardRefresh fires and SHOULD poll every 60s.

#### Scenario: Refresh on dashboard event

- GIVEN the card is displayed with current status data
- WHEN EventBus.OnDashboardRefresh fires
- THEN the card re-fetches status and updates the display

#### Scenario: Periodic polling

- GIVEN the card is displayed
- WHEN 60s elapse without a manual refresh
- THEN the card SHOULD re-fetch status data automatically

---

### Requirement: Architecture Isolation

Status derivation logic MUST reside in the Application layer behind port interfaces.
Readiness adapters MUST reside in Infrastructure. No provider-specific types or
adapter implementations SHALL appear in the Application or UI layers. Architecture
tests MUST enforce this boundary.

#### Scenario: Architecture tests confirm layer isolation

- GIVEN the dashboard-system-status capability is fully implemented
- WHEN the architecture test suite runs
- THEN no Infrastructure or provider-specific types are found in the Application
  or UI project namespaces
