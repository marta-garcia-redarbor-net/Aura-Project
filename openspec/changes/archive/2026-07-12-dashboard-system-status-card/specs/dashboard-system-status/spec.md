# Delta for dashboard-system-status

## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: Status API Endpoint

The API MUST expose a GET-only endpoint returning a DTO with all 5 indicator states (api, db, qdrant, llm, mockAuth). Write verbs (POST, PUT, PATCH, DELETE) MUST NOT be accepted.
(Previously: DTO included api, qdrant, and mockAuth only)

#### Scenario: GET returns all fields

- GIVEN any valid runtime configuration
- WHEN a client sends GET to the system-status endpoint
- THEN the response is HTTP 200 with a DTO containing api, db, qdrant, llm, and mockAuth fields (each with state and microcopy)

#### Scenario: Write verbs rejected

- GIVEN the system-status endpoint is available
- WHEN a client sends POST, PUT, PATCH, or DELETE
- THEN the response is HTTP 405 Method Not Allowed
