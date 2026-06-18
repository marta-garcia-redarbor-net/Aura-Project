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

The API MUST expose a GET-only endpoint returning a DTO with all three indicator
states and their microcopy. Write verbs (POST, PUT, PATCH, DELETE) MUST NOT be
accepted on this endpoint.

#### Scenario: GET returns all indicator states

- GIVEN any valid runtime configuration
- WHEN a client sends GET to the system-status endpoint
- THEN the response is HTTP 200 with a DTO containing `api`, `qdrant`, and `mockAuth`
  fields, each carrying a `state` and `microcopy` value

#### Scenario: Write verbs rejected

- GIVEN the system-status endpoint is available
- WHEN a client sends POST, PUT, PATCH, or DELETE
- THEN the response is HTTP 405 Method Not Allowed

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
